public class TravelResponse
{
    public TravelPromptContext TravelPromptContext { get; set; } = new();
    public TripPlanningData TripPlanningData { get; set; } = new();
    public Itinerary itinerary { get; set; } = new();
}