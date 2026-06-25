public sealed class TripPlanningData
{
    public TravelTime? TravelTime { get; init; }
    public List<Place> RecommendedPlaces { get; init; } = new();
    public List<Place> Hotels { get; init; } = new();
    public List<Place> Restaurants { get; init; } = new();
}