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

    public async Task<List<Place>> GetMapDataAsync(
        string location,
        //add an interest filter
        IEnumerable<string>? interests = null)
    {
        try
        {
            var (lat, lon) = await _utils.GetCoordinatesAsync(location);

            var token = await GetAccessTokenAsync();

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                _searchNearbyUrl);

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            request.Headers.Add(
                "X-Goog-FieldMask",
                "places.displayName," +
                "places.formattedAddress," +
                "places.rating," +
                "places.priceLevel," +
                "places.types," +
                "places.editorialSummary," +
                "places.currentOpeningHours," +
                "places.reviews");

//Add interest filter
            var body = new
            {
                includedTypes = new[] { "tourist_attraction" },
                maxResultCount = 20,
                locationRestriction = new
                {
                    circle = new
                    {
                        center = new
                        {
                            latitude = lat,
                            longitude = lon
                        },
                        radius = 5000
                    }
                }
            };

            request.Content = JsonContent.Create(body);
            request.Content.Headers.ContentType =
                new MediaTypeHeaderValue("application/json");

            var response = await _httpClient.SendAsync(request);

            var raw = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new AppException(
                    "MAP_API_ERROR",
                    $"Google Places failed {(int)response.StatusCode}: {raw}");
            }

            var result =
                JsonSerializer.Deserialize<GooglePlacesResponse>(raw,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            return result?.Places == null
                ? new List<Place>()
                : MapResponse(result.Places);
        }
        catch (Exception ex)
        {
            throw new AppException(
                "MAP_SERVICE_ERROR",
                ex.Message);
        }
    }

    private static List<Place> MapResponse(List<GooglePlace> places)
    {
        return places.Select(p => new Place
        {
            Name = p.DisplayName?.Text ?? "",

            Description =
                p.EditorialSummary?.Text
                ?? string.Join(", ", p.Types ?? []),

            Rating = (int)Math.Round(p.Rating ?? 0),

            Reviews = p.Reviews?.Select(r => new Review
            {
                Rating = r.Rating,
                Text = r.Text?.Text ?? string.Empty
            })
                .ToList()
                ?? [],
            PriceRange = GetPriceRange(p.PriceLevel ?? 0),

            PriceLevel = p.PriceLevel ?? 0,

            OpenTime = p.CurrentOpeningHours?.WeekDayDescriptions ?? [],

            Address = p.FormattedAddress ?? "",
        }).ToList();
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
}