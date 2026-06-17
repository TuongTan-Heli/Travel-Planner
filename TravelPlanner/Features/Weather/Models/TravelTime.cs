public class TravelTime
{
    public string Location { get; init; } = string.Empty;
    public DateTime StartTime { get; init; } = DateTime.MinValue;
    public DateTime EndTime { get; init; } = DateTime.MinValue;

    public List<WeatherDay> WeatherForecasts { get; init; } = new();
}