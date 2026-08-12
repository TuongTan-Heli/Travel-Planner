using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TravelPlanner;

public class Utils
{
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly string STATE_ID = "STATE";

    private readonly HttpClient _httpClient;
    public Utils(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    public DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateTime.TryParseExact(
            value,
            "dd-MM-yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date)
            ? date
            : null;
    }
    public (DateTime Start, DateTime End) GetNextBestTravelWindow(
    int month,
    int week,
    int days,
    DateTime now)
    {
        var year = now.Year;


        var firstDay =
            new DateTime(year, month, 1);


        var start =
            firstDay.AddDays((week - 1) * 7);


        // if already passed, use next year
        if (start < now.Date)
        {
            start =
                new DateTime(year + 1, month, 1)
                .AddDays((week - 1) * 7);
        }


        var end =
            start.AddDays(days - 1);


        return (start, end);
    }

    public DateTime GetDateFromWeekOfYear(int year, int week)
    {
        var firstDay = new DateTime(year, 1, 1);

        var offset = (week - 1) * 7;

        return firstDay.AddDays(offset);
    }

    public async Task<(double lat, double lon)> GetCoordinatesAsync(
    string location)
    {
        var url =
            $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(location)}";

        var response =
            await _httpClient.GetFromJsonAsync<Location>(url);

        var first = response?.Results?.FirstOrDefault();

        if (first == null)
        {
            throw new AppException(
                "LOCATION_NOT_FOUND",
                $"Could not find coordinates for {location}");
        }

        return (first.Latitude, first.Longitude);
    }

    private const double EarthRadiusKm = 6371.0;

    public double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = DegreesToRadians(lat2 - lat1);
        double dLon = DegreesToRadians(lon2 - lon1);

        lat1 = DegreesToRadians(lat1);
        lat2 = DegreesToRadians(lat2);

        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(lat1) * Math.Cos(lat2) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Asin(Math.Sqrt(a));

        return EarthRadiusKm * c;
    }

    public double Haversine(Altitude altitude1, Altitude altitude2)
    {
        var lat1 = altitude1.Latitude;
        var lon1 = altitude1.Longitude;
        var lat2 = altitude2.Latitude;
        var lon2 = altitude2.Longitude;

        double dLat = DegreesToRadians(lat2 - lat1);
        double dLon = DegreesToRadians(lon2 - lon1);

        lat1 = DegreesToRadians(lat1);
        lat2 = DegreesToRadians(lat2);

        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(lat1) * Math.Cos(lat2) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Asin(Math.Sqrt(a));

        return EarthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    public async Task BroadcastStateAsync(
    WebSocket socket,
    bool processing,
    string message)
    {
        await BroadcastAsync(socket, new SystemStateMessage
        {
            Id = STATE_ID,
            Type = WebSocketMessType.State,
            Message = message,
            Processing = processing
        });
    }

    public async Task BroadcastAsync(
    WebSocket socket,
    WebSocketMessage message)
    {
        var payload = JsonSerializer.Serialize(
            message,
            message.GetType(),
            SerializerOptions
        );

        var bytes = Encoding.UTF8.GetBytes(payload);

        await socket.SendAsync(
            bytes,
            WebSocketMessageType.Text,
            true,
            CancellationToken.None
        );
    }
    public (int Min, int Max) GetTravelPlaceCount(
        TravelFrequency? frequency)
    {
        return frequency switch
        {
            TravelFrequency.High => (5, 6),
            TravelFrequency.Medium => (3, 4),
            TravelFrequency.Low => (1, 2),
            _ => (3, 4)
        };
    }

    public (int Min, int Max) GetPlaceCount(
        PlaceCategory category,
        TravelFrequency? frequency)
    {
        return category switch
        {
            PlaceCategory.Travel => frequency switch
            {
                TravelFrequency.High => (5, 6),
                TravelFrequency.Medium => (3, 4),
                TravelFrequency.Low => (1, 2),
                _ => (3, 4)
            },

            PlaceCategory.Restaurant => (2, 3),

            PlaceCategory.Hotel => (1, 1),

            _ => (1, 1)
        };
    }
}