public class Place
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int Rating { get; init; } = 0;
    public List<Review> Reviews { get; init; } = new();
    public string PriceRange { get; init; } = string.Empty;
    public int PriceLevel { get; init; } = 0;
    public List<string> OpenTime { get; init; } = [];
    public string Address { get; init; } = string.Empty;
}

public class Review
{
    public int Rating { get; init; }
    public string Text { get; init; } = string.Empty;
}