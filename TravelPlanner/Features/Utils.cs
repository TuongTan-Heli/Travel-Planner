using System.Globalization;

namespace TravelPlanner;

public class Utils
{
    public static DateTime? ParseDate(string? value)
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
    public static (DateTime Start, DateTime End) GetNextBestTravelWindow(
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

    // assume you already have this somewhere
    public static DateTime GetDateFromWeekOfYear(int year, int week)
    {
        var firstDay = new DateTime(year, 1, 1);

        var offset = (week - 1) * 7;

        return firstDay.AddDays(offset);
    }

}