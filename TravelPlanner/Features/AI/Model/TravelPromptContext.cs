public class TravelPromptContext
{
    public string? Destination { get; set; }

    public int? Days { get; set; }

    public decimal? Budget { get; set; }

    public int? Travelers { get; set; }
    public bool IsReadyForPlanning()
    {
        return !string.IsNullOrWhiteSpace(Destination)
               && Days.HasValue
               && Budget.HasValue
               && Travelers.HasValue;
    }

}