using TravelPlanner.Features.Map.Model;

public class TravelIntentResult
{
    public string? Destination { get; set; }
    public string? Country { get; set; }
    public int? Days { get; set; }
    public Money? Budget { get; set; }
    public int? Travelers { get; set; }
    public bool IsTravelRelated { get; set; }
    public bool IsReadyForPlanning { get; set; }
    public String? StartDate { get; set; }
    public String? EndDate { get; set; }
    public List<string> Interests { get; set; } = [];
    public List<string> Preferences { get; set; } = [];
    public double? MinRating { get; set; } = null;
    public string AssistantMessage { get; set; } = "";
    public string NextAction { get; set; } = "";
    public TravelFrequency? TravelFrequency { get; set; } = null;
}

public enum TravelFrequency
{
    High,
    Medium,
    Low
}
