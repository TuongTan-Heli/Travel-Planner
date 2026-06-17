public sealed class TripPlanningData
{
    public TravelTime? TravelTime { get; init; }
    public List<PlaceRecommendation> RecommendedPlaces { get; init; } = new();
    // public List<HotelRecommendation> Hotels { get; init; } = new();
    // public List<RestaurantRecommendation> Restaurants { get; init; } = new();
    // public List<TimeRecommendation> TimeRecommendations { get; init; } = new();
}