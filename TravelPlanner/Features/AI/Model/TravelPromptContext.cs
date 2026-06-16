public class TravelPromptContext
{
    public string? Destination { get; set; }
    public int? Days { get; set; }
    public decimal? Budget { get; set; }
    public int? Travelers { get; set; }
    public bool IsReadyForPlanning()
    {
        return !string.IsNullOrWhiteSpace(Destination)
                  && (Days.HasValue || (StartDate != null && EndDate != null))
               && Budget.HasValue;
    }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string> Interests { get; set; } = [];
    public List<string> Preferences { get; set; } = [];

}