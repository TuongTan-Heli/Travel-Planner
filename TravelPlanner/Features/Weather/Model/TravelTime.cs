public class TravelTime
{
    public string Location { get; init; } = string.Empty;

    public DateTime StartTime { get; init; }

    public DateTime EndTime { get; init; }

    public double WeatherScore { get; init; }

    public List<LocationForecast> Forecasts { get; init; } = [];
}


public class LocationForecast
{
    public string Location { get; init; } = string.Empty;

    public List<WeatherDay> Days { get; init; } = [];
}