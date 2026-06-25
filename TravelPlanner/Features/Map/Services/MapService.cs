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
    private readonly List<string> restaurantFilter = ["restaurant", "food", "fast_food_restaurant"];

    private readonly List<string> attractionFilter = ["tourist_attraction", "point_of_interest"];

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
            #endregion 

            #region enrich call
            if (context.Days >= 5 || context.Interests.Count >= 3)
            {
                var enrichCall = await GetPlacesAsync(
                                    lat,
                                    lon,
                                    token,
                                    30000,
                                    MapVariables.PrimaryTypes,
                                    BuildInterests(context.Interests)
                                    );

                places.AddRange(enrichCall);
            }
            #endregion

            #region coverage call
            var total = places.Count;

            var hotelCount = places.Count(x => x.Category == PlaceCategory.Hotel);
            var restaurantCount = places.Count(x => x.Category == PlaceCategory.Restaurant);
            var travelCount = places.Count(x => x.Category == PlaceCategory.Travel);

            var hotelRatio = (double)hotelCount / total;
            var restaurantRatio = (double)restaurantCount / total;
            var travelRatio = (double)travelCount / total;

            bool needHotels = hotelRatio < 0.10;
            bool needRestaurants = restaurantRatio < 0.30;
            bool needTravel = travelRatio < 0.60;

            var missingInterests = GetMissingInterests(places, context.Interests);

            #endregion

            #region sub city result expand call

            #endregion


            #region far city result expand call

            #endregion
            places = places
                .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            
            return places;
        }
        catch (Exception ex)
        {
            throw new AppException(
                "MAP_SERVICE_ERROR",
                ex.ToString());
        }
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
            PriceLevel = p.PriceLevel ?? 0,
            PriceRange = GetPriceRange(p.PriceLevel ?? 0),
            Reviews = p.Reviews?
                .Select(r => new Review
                {
                    Rating = r.Rating,
                    Text = r.Text?.Text ?? ""
                })
                .ToList()
                ?? [],
            ReviewSummary = p.ReviewSummary?.Text ?? string.Empty,
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

    private static List<string> DetermineInterests(
    IEnumerable<string> types)
    {
        return MapVariables.InterestTypes
            .Where(x => x.Value.Any(types.Contains))
            .Select(x => x.Key)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string GetPriceRange(int level)
    {
        return level switch
        {
            0 => "Free",
            1 => "$",
            2 => "$$",
            3 => "$$$",
            4 => "$$$$",
            _ => ""
        };
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
}