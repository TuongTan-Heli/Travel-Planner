public class MapService
{
    public async Task<List<PlaceRecommendation>> GetMapDataAsync(string location, IEnumerable<string>? interests = null)
    {
        // Simulate fetching map data with a delay
        return await Task.FromResult(new List<PlaceRecommendation>
        {
            new PlaceRecommendation { Name = "Central Park", Description = "A large public park in New York City." },
            new PlaceRecommendation { Name = "Statue of Liberty", Description = "An iconic symbol of freedom in the United States." },
            new PlaceRecommendation { Name = "Times Square", Description = "A major commercial intersection and tourist destination." }
        });
    }
}