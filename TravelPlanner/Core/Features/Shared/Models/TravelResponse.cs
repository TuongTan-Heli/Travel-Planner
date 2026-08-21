using TravelPlanner.Features.AI.Model;
public class TravelResponse
{
    // public TravelPromptContext TravelPromptContext { get; set; } = new();
    public TripPlanningData TripPlanningData { get; set; } = new();
    public Itinerary Itinerary { get; set; } = new();
    public FinalPresentation FinalPresentation { get; set; } = new();
}