public class TravelTime
{
    // public PlaceCluster Location { get; init; } = new();

    public DateTime StartTime { get; init; }

    public DateTime EndTime { get; init; }

    public double WeatherScore { get; init; }

    public List<LocationForecast> Forecasts { get; init; } = [];
}


public class LocationForecast
{
    public Altitude Location { get; init; } = new();

    public List<WeatherDay> Days { get; init; } = [];
}