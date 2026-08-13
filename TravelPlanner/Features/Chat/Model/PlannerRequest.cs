using System.Text.Json.Serialization;
using TravelPlanner.Features.Map.Model;

namespace TravelPlanner.Features.Chat.Models;

public class PlannerRequest
{
    public string Destination { get; set; } = string.Empty;

    public decimal Budget { get; set; }

    public string Currency { get; set; } = "AUD";

    public string? StartDate { get; set; }

    public string? EndDate { get; set; }

    public int? Days { get; set; }

    public int? Travelers { get; set; }

    public double? Rating { get; set; }

    public List<string> Interests { get; set; } = [];

    public List<string> Preferences { get; set; } = [];
    
    [JsonPropertyName("frequency")]
    public TravelFrequency? TravelFrequency { get; set; }

    public void ApplyTo(TravelSession session)
    {
        session.Context.Destination = Destination;
        session.Context.Country = Destination;

        if (session.Context.Budget == null)
        {
            session.Context.Budget = new Money();
        }

        session.Context.Budget.Units = Budget;
        session.Context.Budget.CurrencyCode = Currency;

        session.Context.StartDate = ParseDate(StartDate);
        session.Context.EndDate = ParseDate(EndDate);

        session.Context.Days = Days;

        session.Context.Travelers = Travelers;

        session.Context.Interests = Interests;

        session.Context.Preferences = Preferences;

        session.Context.MinRating = Rating;

        session.Context.TravelFrequency = TravelFrequency;
    }

    private static DateTime? ParseDate(string? date)
    {
        if (string.IsNullOrWhiteSpace(date))
            return null;

        return DateTime.TryParseExact(
            date,
            "yyyy-MM-dd",
            null,
            System.Globalization.DateTimeStyles.None,
            out var result)
            ? result
            : null;
    }
}