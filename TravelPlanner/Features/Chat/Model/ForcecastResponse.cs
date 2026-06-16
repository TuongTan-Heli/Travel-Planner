public sealed class ForecastResponse
{
    public HourlyForecast Hourly { get; set; } = new();
}

public sealed class HourlyForecast
{
    public List<DateTime> Time { get; set; } = [];
    public List<double> Temperature_2m { get; set; } = [];
    public List<int> Precipitation_Probability { get; set; } = [];
}