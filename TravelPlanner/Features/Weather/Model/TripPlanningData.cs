public class TripPlanningData
{
    public TravelTime? TravelTime { get; init; }
    public List<Place> RecommendedPlaces { get; set; } = new();
}