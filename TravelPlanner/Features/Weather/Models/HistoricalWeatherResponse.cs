public class HistoricalWeatherResponse
{
    public HistoricalDaily Daily { get; set; } = new();
}

public class HistoricalDaily
{
    public List<DateTime> Time { get; set; } = [];

    public List<double> Temperature_2m_Max { get; set; } = [];

    public List<double> Temperature_2m_Min { get; set; } = [];

    public List<double> Precipitation_Sum { get; set; } = [];

    public List<int> Weather_Code { get; set; } = [];
}