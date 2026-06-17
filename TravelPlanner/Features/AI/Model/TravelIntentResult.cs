public class TravelIntentResult
{
    public string? Destination { get; set; }
    public int? Days { get; set; }
    public decimal? Budget { get; set; }
    public string? Currency {get; set;}
    public int? Travelers { get; set; }
    public bool IsTravelRelated { get; set; }
    public bool IsReadyForPlanning { get; set; }
    public String? StartDate { get; set; }
    public String? EndDate { get; set; }
    public List<string> Interests { get; set; } = [];
    public List<string> Preferences { get; set; } = [];
    public List<string> MissingFields { get; set; } = [];
    public string AssistantMessage { get; set; } = "";
    public string NextAction { get; set; } = "";
}