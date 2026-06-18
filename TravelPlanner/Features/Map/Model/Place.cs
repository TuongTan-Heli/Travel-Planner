public class Place
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int Rating { get; init; } = 0;
    public List<string> Reviews { get; init; } = new();
    public string PriceRange { get; init; } = string.Empty;
    public int PriceLevel { get; init; } = 0;
    public TimeSpan OpenTime { get; init; } = new();
    public TimeSpan CloseTime { get; init; } = new();
    public string Address { get; init; } = string.Empty;
}