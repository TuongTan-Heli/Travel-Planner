using System.Net.Http.Headers;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using TravelPlanner.Features.Chat.Services;
using TravelPlanner.Features.Map.Model;

namespace TravelPlanner.Features.Map;

public class MapService
{
    private readonly HttpClient _httpClient;
    private readonly Utils _utils;
    private readonly PlanningBudgetService _planningBudgetService;
    private readonly CurrencyExchangeService _currencyExchangeService;
    private readonly string _path;
    private readonly string _searchNearbyUrl;
    private readonly string _searchTextUrl;
    private const int maxAttemptCall = 5;

    private static readonly Dictionary<PlaceCategory, int> ClusterMinimum = new()
    {
        { PlaceCategory.Travel, 5 },
        { PlaceCategory.Restaurant, 3 },
        { PlaceCategory.Hotel, 2 }
    };


    public MapService(HttpClient httpClient, Utils utils, PlanningBudgetService planningBudgetService, CurrencyExchangeService currencyExchangeService)
    {
        _httpClient = httpClient;
        _utils = utils;
        _planningBudgetService = planningBudgetService;
        _currencyExchangeService = currencyExchangeService;
        _searchNearbyUrl = Environment.GetEnvironmentVariable("GOOGLE_MAP_SEARCH_NEARBY_API_URL") ?? string.Empty;
        _searchTextUrl = Environment.GetEnvironmentVariable("GOOGLE_MAP_SEARCH_TEXT_API_URL") ?? string.Empty;
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

    public async Task<List<Place>> GetMapDataAsync(List<PlaceCluster> clusters, TravelSession session)
    {
        try
        {
            var token = await GetAccessTokenAsync();

            var clusterPlaces = new List<List<Place>>();

            foreach (var cluster in clusters)
            {
                var places = await GetCorePlaces(
                    cluster,
                    token,
                    session);

                clusterPlaces.Add(places);
            }


            for (int i = 0; i < clusters.Count; i++)
            {
                await FillCluster(
                    clusters[i],
                    clusterPlaces[i],
                    token,
                    session);
            }


            return clusterPlaces
                .SelectMany(x => x)
                .ToList();

        }
        catch (AppException ex)
        {
            throw new AppException(
                "MAP_SERVICE_ERROR",
                "Failed to retrieve map, location data.",
                ex.Message);
        }
    }

    private async Task<List<Place>> GetCorePlaces(
    PlaceCluster cluster,
    string token,
    TravelSession session)
    {
        var result = new List<Place>();

        var types = new[]
        {
        MapVariables.PrimaryActtractionTypes,
        MapVariables.PrimaryRestaurantTypes,
    };

        foreach (var type in types)
        {
            var places = await GetPlacesAsync(
                cluster,
                cluster.Center.Latitude,
                cluster.Center.Longitude,
                token,
                50000,
                session,
                type,
                []);


            result = MergePlaces(result, places);
        }

        return result;
    }

    private async Task FillCluster(
    PlaceCluster cluster,
    List<Place> places,
    string token,
    TravelSession session)
    {

        var attempts = 0;

        while (attempts < 5)
        {
            var missing = GetMissingCategories(places);

            if (!missing.Any())
                break;

            foreach (var category in missing)
            {

                var types = category switch
                {
                    PlaceCategory.Hotel =>
                        MapVariables.PrimaryHotelTypes,

                    PlaceCategory.Restaurant =>
                        MapVariables.PrimaryRestaurantTypes,

                    _ =>
                        MapVariables.PrimaryTravelTypes
                };


                var results = await GetPlacesAsync(
                    cluster,
                    cluster.Center.Latitude,
                    cluster.Center.Longitude,
                    token,
                    GetRadius(category),
                    session,
                    types,
                    category == PlaceCategory.Travel ? BuildInterests(session.Context.Interests) : []);
                places.AddRange(
                    MergePlaces([], results)
                );
            }


            attempts++;
        }
    }

    private List<PlaceCategory> GetMissingCategories(
    List<Place> places)
    {
        return ClusterMinimum
            .Where(x =>
                places.Count(p => p.Category == x.Key)
                < x.Value)
            .Select(x => x.Key)
            .ToList();
    }

    private int GetRadius(PlaceCategory category)
    {
        return category switch
        {
            PlaceCategory.Hotel => Random.Next(1, 10) * 2000,

            PlaceCategory.Restaurant => Random.Next(1, 15) * 2000,

            PlaceCategory.Travel => Random.Next(1, 25) * 2000,

            _ => 10000
        };
    }

    private static List<Place> MergePlaces(IReadOnlyCollection<Place> existingPlaces, IEnumerable<Place> newPlaces)
    {
        var merged = new List<Place>(existingPlaces);

        foreach (var place in newPlaces)
        {
            if (merged.Any(x => string.Equals(x.Name, place.Name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            merged.Add(place);
        }

        return merged;
    }
    private async Task<List<Place>> GetPlacesAsync(
    PlaceCluster cluster,
    double lat,
    double lon,
    string token,
    int rad,
    TravelSession session,
    List<string> primaryTypes,
    List<string>? types = null,
    double? minRating = 3)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _searchNearbyUrl);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        request.Headers.Add("X-Goog-FieldMask", MapVariables.GoogleMapFieldMask);

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

        request.Content = JsonContent.Create(body);

        var response = await _httpClient.SendAsync(request);

        var raw = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new AppException(
                "MAP_API_ERROR",
                "Something went wrong while retrieving data from Google Places API, please try again later.",
                $"Google Places failed {(int)response.StatusCode}: {raw}");
        }

        var result = JsonSerializer.Deserialize<GooglePlacesResponse>(
                raw,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        var places = result?.Places == null ? [] : MapResponse(result.Places, cluster);

        if (minRating.HasValue)
        {
            places = places
                .Where(p => p.Rating >= minRating.Value)
                .ToList();
        }

        places = await FilterByBudgetAsync(places, session.Context);

        return places;
    }

    private async Task<List<Place>> FilterByBudgetAsync(
    List<Place> places,
    TravelPromptContext context)
    {
        var result = new List<Place>();

        foreach (var place in places)
        {
            if (await IsWithinBudgetRangeAsync(place, context))
            {
                result.Add(place);
            }
        }

        return result;
    }

    private async Task<bool> IsWithinBudgetRangeAsync(
    Place place,
    TravelPromptContext context)
    {
        if (context.Budget?.Units is null ||
            context.Days is null ||
            context.Days <= 0)
        {
            return true;
        }

        if (place.PriceRange?.StartPrice?.Units is null ||
            place.PriceRange?.EndPrice?.Units is null)
        {
            return true;
        }

        var targetCurrency = place.PriceRange.StartPrice.CurrencyCode
            ?? context.Budget.CurrencyCode
            ?? "USD";

        var budgetCurrency = context.Budget.CurrencyCode ?? "USD";

        var convertedBudget = await _currencyExchangeService.ConvertAsync(
                context.Budget.Units,
                budgetCurrency,
                targetCurrency);

        var dailyBudget = convertedBudget / context.Days.Value;

        var allocation = _planningBudgetService.GetBudgetAllocation(place.Category);

        var travelers = context.Travelers ?? 1;

        var maxBudget = dailyBudget * allocation.Max / travelers;

        const decimal tolerance = 1.5m;

        var allowedMaximum = maxBudget * tolerance;

        var avgPrice = (place.PriceRange.StartPrice.Units + place.PriceRange.EndPrice.Units) / 2m;

        return avgPrice <= allowedMaximum;
    }

    private static List<Place> MapResponse(List<GooglePlace> places, PlaceCluster? cluster)
    {
        return [.. places.Select(p => new Place
        {
            Name = p.DisplayName?.Text ?? "",
            Address = p.FormattedAddress ?? "",
            Location = p.Location,
            Country = p.AddressComponents.FirstOrDefault(x => x.Types.Contains("country"))?.LongText ?? "",
            Photos = p.Photos?
                    .Select(x => x.Name)
                    .ToList()
                    ?? [],
            PrimaryType = p.PrimaryType,
            Types = p.Types ?? [],
            Rating = p.Rating ?? 0,
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
            Takeout = p.Takeout,
            PlaceCluster = cluster
        })];
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

    private static List<Place> FilterInterestingPlaces(
    string country,
    List<Place> places,
    double keepPercentage = 0.8,
    int minimumKeep = 15)
    {
        var ranked = places
            .Where(x => x.Address.Contains(country) || x.Country.Contains(country))
            .Select(p => new
            {
                Place = p,
                Score = CalculateInterestScore(p)
            })
            .OrderByDescending(x => x.Score)
            .ToList();

        if (ranked.Count <= minimumKeep)
        {
            return ranked
                .Select(x => x.Place)
                .ToList();
        }

        var keep = Math.Max(minimumKeep, (int)Math.Ceiling(ranked.Count * keepPercentage));

        return ranked
            .Take(keep)
            .Select(x => x.Place)
            .ToList();
    }

    private static double CalculateInterestScore(Place place)
    {
        const double AverageRating = 4.0;
        const double MinimumVotes = 500;

        double rating = place.Rating;
        double votes = place.UserRatingCount;

        // Bayesian weighted rating
        double weightedRating =
            votes / (votes + MinimumVotes) * rating +
            MinimumVotes / (votes + MinimumVotes) * AverageRating;

        // Logarithmic popularity bonus
        double popularity =
            Math.Log10(votes + 1);

        // Final score
        return weightedRating * popularity;
    }

    public async Task<List<PlaceCluster>> GetLocations(
    TravelSession session)
    {
        var location = session.Context.Destination ?? throw new AppException(
            "MAP_INSUF_DATA",
            "Destination is required for planning.",
            "Destination is required for planning.");
        var country = session.Context.Country ?? throw new AppException(
            "MAP_INSUF_DATA",
            "Country is required for planning.",
            "Country is required for planning.");
        var days = session.Context.Days ?? 1;

        var token = await GetAccessTokenAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, _searchTextUrl);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-Goog-FieldMask", MapVariables.GoogleMapFieldMaskLocations);

        request.Content = JsonContent.Create(new
        {
            textQuery = $"{location} Travel attractions in {country}"
        });

        var response = await _httpClient.SendAsync(request);
        var raw = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new AppException(
                "MAP_API_ERROR",
                "Something went wrong while retrieving data from Google Places API, please try again later.",
                $"Google Places failed {(int)response.StatusCode}: {raw}");
        }

        var result = JsonSerializer.Deserialize<GooglePlacesResponse>(
            raw,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var places = result?.Places == null ? [] : MapResponse(result.Places, null);

        var prioritizedPlaces = FilterInterestingPlaces(country, places);

        if (!prioritizedPlaces.Any())
        {
            throw new AppException(
                "MAP_API_ERROR",
                "Found no places for the given destination and country.",
                $"Google Places returned no places for '{location}, {country}'.");
        }

        var clusters = new List<PlaceCluster>();

        foreach (var place in prioritizedPlaces)
        {
            var cluster = clusters.FirstOrDefault(c =>
                c.Places.Any(p => _utils.Haversine(p.Location, place.Location) <= 100));

            if (cluster == null)
            {
                cluster = new PlaceCluster();
                clusters.Add(cluster);
            }
            cluster.Places.Add(place);
        }

        foreach (var cluster in clusters)
        {
            // cluster.Places = await _scoringService.ScorePlaces(cluster.Places, session);
            var center = cluster.Places.OrderByDescending(p => p.Rating).First();

            cluster.Center = center.Location;
        }

        var clusterCount = Math.Min(clusters.Count, Math.Max(1, (int)Math.Ceiling(days / 3.5)));

        return clusters
            .OrderBy(_ => Random.Next())
            .Take(clusterCount)
            .ToList();
    }
}