using System.Globalization;

namespace TravelPlanner;

public class Utils
{
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
        int bestWeek,
        int days,
        DateTime? fromDate = null)
    {
        var today = (fromDate ?? DateTime.UtcNow).Date;

        var years = new[]
        {
            today.Year,
            today.Year + 1,
            today.Year + 2
        };

        var candidates = new List<DateTime>();

        foreach (var year in years)
        {
            var date = GetDateFromWeekOfYear(year, bestWeek);

            if (date >= today)
                candidates.Add(date);
        }

        if (!candidates.Any())
            throw new InvalidOperationException("No valid travel window found.");

        var start = candidates
            .OrderBy(x => x)
            .First();

        var end = start.AddDays(days - 1);

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

}