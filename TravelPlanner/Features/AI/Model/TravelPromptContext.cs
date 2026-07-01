using TravelPlanner.Features.Map.Model;

public class TravelPromptContext
{
    public string? Destination { get; set; }
    public int? Days { get; set; }
    public Money? Budget { get; set; }
    public int? Travelers { get; set; }
    public bool IsReadyForPlanning()
    {
        return !string.IsNullOrWhiteSpace(Destination)
       && (Days.HasValue || (StartDate != null && EndDate != null))
        && Budget?.Units > 0
        && !string.IsNullOrWhiteSpace(Budget?.CurrencyCode);
        // Destination = "Ho Chi Minh city";
        // Budget = 10000;
        // Days = 5;
        // return true;
    }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string> Interests { get; set; } = [];
    public List<string> Preferences { get; set; } = [];

}