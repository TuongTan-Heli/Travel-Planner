public sealed class ForecastResponse
{
    public HourlyForecast Hourly { get; set; } = new();

    public DailyForecast Daily { get; set; } = new();
}

public sealed class HourlyForecast
{
    public List<DateTime> Time { get; set; } = [];
    public List<double> Temperature_2m { get; set; } = [];
    public List<int> Precipitation_Probability { get; set; } = [];

}

public sealed class DailyForecast
{
    public List<DateTime> Time { get; set; } = [];

    public List<double> Temperature_2m_Max { get; set; } = [];

    public List<double> Temperature_2m_Min { get; set; } = [];

    public List<double> Precipitation_Sum { get; set; } = [];

    public List<int> Weather_Code { get; set; } = [];

    public List<double> Precipitation_Probability_Max { get; set; } = [];
}
