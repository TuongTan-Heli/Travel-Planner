using System.Net.Http.Headers;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using TravelPlanner.Features.Map.Model;

namespace TravelPlanner.Features.Map;

public class MapService
{
    private readonly HttpClient _httpClient;
    private readonly Utils _utils;
    private readonly string _path;
    private readonly string _searchNearbyUrl;
    private const int maxAttemptCall = 5;

    public MapService(HttpClient httpClient, Utils utils)
    {
        _httpClient = httpClient;
        _utils = utils;
        _searchNearbyUrl = Environment.GetEnvironmentVariable("GOOGLE_MAP_SEARCH_NEARBY_API_URL") ?? string.Empty;
        _path = Environment.GetEnvironmentVariable("GOOGLE_ACCESS_PATH") ?? string.Empty;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var serviceCredential =
        (await CredentialFactory
            .FromFileAsync<ServiceAccountCredential>(_path, CancellationToken.None))
        .ToGoogleCredential();

        var scoped =
        serviceCredential.CreateScoped(
            "https://www.googleapis.com/auth/cloud-platform");

        return await scoped.UnderlyingCredential
            .GetAccessTokenForRequestAsync();
    }

    public async Task<List<Place>> GetMapDataAsync(TravelPromptContext context)
    {
        int AttemptCall = 0;
        int consecutiveDuplicateCalls = 0;
        try
        {
            List<Place> places = new List<Place>();
            var (lat, lon) =
                await _utils.GetCoordinatesAsync(
                    context.Destination ?? "");

            var token =
                await GetAccessTokenAsync();

            #region call 1 core city call
            var coreCall = await GetPlacesAsync(
                        lat,
                        lon,
                        token,
                        10000,
                        MapVariables.PrimaryTypes,
                        BuildInterests(context.Interests)
                        );
            places.AddRange(coreCall
                            .GroupBy(x => x.Name)
                            .Select(x => x.First())
                            .ToList());
            //update thinking meessage with thinkingId
            AttemptCall++;
            #endregion
            while (AttemptCall < maxAttemptCall)
            {
                var missingInterests =
                    GetMissingInterests(places, context.Interests);

                var missingTypes =
                    GetMissingPrimaryTypes(places, context);
                var interestCoverage = GetInterestCoverage(
                    places,
                    context.Interests);

                if (interestCoverage >= 0.7 &&
                    !missingTypes.Any() &&
                    places.Count >= context.Days * 4)
                {
                    break;
                }

                var before = places.Count;
                var (newLat, newLon) = GetRandomCenter(lat, lon, Random.Next(2, 11) * 1000);
                var results = await GetPlacesAsync(
                    newLat,
                    newLon,
                    token,
                    Random.Next(2, 11) * 5000,
                    missingTypes.Any()
                        ? missingTypes
                        : MapVariables.PrimaryTravelTypes,
                    consecutiveDuplicateCalls >= 2 ? BuildRandomInterests([]) : missingInterests.Any()
                        ? BuildRandomInterests(missingInterests)
                        : BuildRandomInterests(context.Interests)
                );

                places = places
                        .Concat(results)
                        .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                        .Select(g => g.First())
                        .ToList();

                if (places.Count == before)
                {
                    consecutiveDuplicateCalls++;
                }

                AttemptCall++;
            }
            return places;
        }
        catch (AppException ex)
        {
            throw new AppException(
                "MAP_SERVICE_ERROR",
                ex.ToString());
        }
    }
    private List<string> GetMissingPrimaryTypes(
    List<Place> places,
    TravelPromptContext context)
    {
        var missing = new List<string>();

        var travelCount =
            places.Count(x => x.Category == PlaceCategory.Travel);

        var restaurantCount =
            places.Count(x => x.Category == PlaceCategory.Restaurant);

        var hotelCount =
            places.Count(x => x.Category == PlaceCategory.Hotel);

        // Capacity requirements
        var requiredTravel = context.Days * 4; //50%
        var requiredRestaurants = context.Days * 3; //37.5%
        var requiredHotels = context.Days; //12.5%

        if (travelCount < requiredTravel)
        {
            missing.AddRange(MapVariables.PrimaryTravelTypes);
        }

        if (restaurantCount < requiredRestaurants)
        {
            missing.AddRange(MapVariables.PrimaryRestaurantTypes);
        }

        if (hotelCount < requiredHotels)
        {
            missing.AddRange(MapVariables.PrimaryHotelTypes);
        }

        return missing
            .Distinct()
            .ToList();
    }

    private double GetInterestCoverage(
    List<Place> places,
    List<string> interests)
    {
        if (interests == null || interests.Count == 0)
            return 1.0;

        var covered = interests.Count(interest =>
        {
            if (!MapVariables.InterestTypes.TryGetValue(interest, out var types))
                return false;

            return places.Any(place =>
                place.Types.Any(type => types.Contains(type)));
        });

        return (double)covered / interests.Count;
    }

    private static (double Latitude, double Longitude) GetRandomCenter(
        double latitude,
        double longitude,
        int maxRadiusMeters)
    {
        const double EarthRadius = 6378137.0; // meters

        // Random distance (uniform over area)
        var distance = Math.Sqrt(Random.NextDouble()) * maxRadiusMeters;

        // Random direction
        var bearing = Random.NextDouble() * 2 * Math.PI;

        var latRad = latitude * Math.PI / 180.0;
        var lonRad = longitude * Math.PI / 180.0;

        var angularDistance = distance / EarthRadius;

        var newLat = Math.Asin(
            Math.Sin(latRad) * Math.Cos(angularDistance) +
            Math.Cos(latRad) * Math.Sin(angularDistance) * Math.Cos(bearing));

        var newLon = lonRad + Math.Atan2(
            Math.Sin(bearing) * Math.Sin(angularDistance) * Math.Cos(latRad),
            Math.Cos(angularDistance) -
            Math.Sin(latRad) * Math.Sin(newLat));

        return (
            newLat * 180.0 / Math.PI,
            newLon * 180.0 / Math.PI
        );
    }
    private async Task<List<Place>> GetPlacesAsync(
    double lat,
    double lon,
    string token,
    int rad,
    List<string> primaryTypes,
    List<string>? types = null)
    {
        var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                _searchNearbyUrl);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        request.Headers.Add(
            "X-Goog-FieldMask",
            MapVariables.GoogleMapFieldMask);

        var body = new
        {
            includedPrimaryTypes = primaryTypes,

            includedTypes = types,

            locationRestriction = new
            {
                circle = new
                {
                    center = new
                    {
                        latitude = lat,
                        longitude = lon
                    },
                    radius = rad
                }
            }
        };

        request.Content =
            JsonContent.Create(body);

        var response =
            await _httpClient.SendAsync(request);

        var raw =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new AppException(
                "MAP_API_ERROR",
                $"Google Places failed {(int)response.StatusCode}: {raw}");
        }

        var result =
            JsonSerializer.Deserialize<GooglePlacesResponse>(
                raw,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        return result?.Places == null
            ? []
            : MapResponse(result.Places);
    }

    private List<string> GetMissingInterests(List<Place> places, List<string> interests)
    {
        var missingInterests = new List<string>();
        foreach (var interest in interests)
        {
            var interestTypes =
                MapVariables.InterestTypes[interest];

            var count =
                places.Count(p =>
                    p.Types.Any(t => interestTypes.Contains(t)));

            if (count == 0)
            {
                missingInterests.Add(interest);
            }
        }
        return missingInterests;
    }

    private static List<Place> MapResponse(List<GooglePlace> places)
    {
        return places.Select(p => new Place
        {
            Name = p.DisplayName?.Text ?? "",
            Address = p.FormattedAddress ?? "",
            Location = p.Location,
            Photos = p.Photos?
                    .Select(x => x.Name)
                    .ToList()
                    ?? [],
            PrimaryType = p.PrimaryType,
            Types = p.Types ?? [],
            Rating = (int)Math.Round(p.Rating ?? 0),
            UserRatingCount = p.UserRatingCount ?? 0,
            OpenTime = p.CurrentOpeningHours?.WeekDayDescriptions ?? [],
            PriceLevel = p.PriceLevel ?? "",
            PriceRange = p.PriceRange is null ? null : new PriceRange
            {
                StartPrice = new Money
                {
                    CurrencyCode = p.PriceRange?.StartPrice?.CurrencyCode ?? "",
                    Units = decimal.TryParse(p.PriceRange?.StartPrice?.Units, out var s) ? s : 0
                },
                EndPrice = new Money
                {
                    CurrencyCode = p.PriceRange?.EndPrice?.CurrencyCode ?? "",
                    Units = decimal.TryParse(p.PriceRange?.EndPrice?.Units, out var e) ? e : 0
                }
            },
            Reviews = p.Reviews?
                .Select(r => new Review
                {
                    Rating = r.Rating,
                    Text = r.Text?.Text ?? ""
                })
                .ToList()
                ?? [],
            ReviewSummary = p.ReviewSummary?.Text?.Text ?? "",
            PhoneNumber = p.InternationalPhoneNumber ?? "",
            WebsiteUrl = p.WebsiteUri ?? "",
            DineIn = p.DineIn,
            AllowsDogs = p.AllowsDogs,
            GoodForChildren = p.GoodForChildren,
            GoodForGroups = p.GoodForGroups,
            GoodForWatchingSports = p.GoodForWatchingSports,
            LiveMusic = p.LiveMusic,
            PaymentOptions = p.PaymentOptions,
            OutdoorSeating = p.OutdoorSeating,
            Reservable = p.Reservable,
            Description =
                p.EditorialSummary?.Text
                ?? string.Join(", ", p.Types ?? []),
            Category = GetCategory(p),
            ServesBeer = p.ServesBeer,
            ServesBreakfast = p.ServesBreakfast,
            ServesBrunch = p.ServesBrunch,
            ServesCocktails = p.ServesCocktails,
            ServesCoffee = p.ServesCoffee,
            ServesDessert = p.ServesDessert,
            ServesDinner = p.ServesDinner,
            ServesLunch = p.ServesLunch,
            ServesVegetarianFood = p.ServesVegetarianFood,
            ServesWine = p.ServesWine,
            Takeout = p.Takeout
        }).ToList();
    }

    private static PlaceCategory GetCategory(GooglePlace place)
    {
        var types = place.PrimaryType;

        if (types.Contains("hotel") ||
            types.Contains("resort_hotel"))
        {
            return PlaceCategory.Hotel;
        }

        if (types.Contains("restaurant") ||
            types.Contains("cafe"))
        {
            return PlaceCategory.Restaurant;
        }

        return PlaceCategory.Travel;
    }

    private static List<string> BuildInterests(IEnumerable<string>? interests)
    {
        if (interests == null || !interests.Any())
        {
            return MapVariables.DefaultTypes.ToList();
        }

        return interests
        .Where(MapVariables.InterestTypes.ContainsKey)
        .SelectMany(i => MapVariables.InterestTypes[i])
        .Distinct()
        .Take(50)
        .ToList();
    }
    private static readonly Random Random = new();

    private static List<string> BuildRandomInterests(
        IEnumerable<string>? interests,
        int minPerGroup = 2,
        int maxPerGroup = 4)
    {
        if (interests == null || !interests.Any())
        {
            return MapVariables.DefaultTypes
                .OrderBy(_ => Random.Next())
                .Take(50)
                .ToList();
        }

        var result = new List<string>();

        foreach (var interest in interests)
        {
            if (!MapVariables.InterestTypes.TryGetValue(interest, out var types))
                continue;

            var count = Math.Min(
                Random.Next(minPerGroup, maxPerGroup + 1),
                types.Length);

            result.AddRange(
                types
                    .OrderBy(_ => Random.Next())
                    .Take(count));
        }

        return result
            .Distinct()
            .OrderBy(_ => Random.Next())
            .Take(50)
            .ToList();
    }
}
