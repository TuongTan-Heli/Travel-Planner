public class TravelSessionRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    public TravelSession Session { get; set; } = new();

    public List<string> ChatHistory { get; set; } = [];

    public bool Success { get; set; }

    public string? Error { get; set; }
}