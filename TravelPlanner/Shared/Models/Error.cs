public class Error
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Path { get; set; }
}