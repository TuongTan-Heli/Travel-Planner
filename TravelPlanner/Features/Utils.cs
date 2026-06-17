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
    public static DateTime FirstDateOfWeekInMonth(
    int year,
    int month,
    int week)
    {
        var firstDay = new DateTime(year, month, 1);

        var offset =
            ((week - 1) * 7);

        return firstDay.AddDays(offset);
    }
}