public class WeatherForecast
{
    public string Name { get; init; } = string.Empty;
    public string Weather { get; init; } = string.Empty;
    public DateTime Time { get; init; } = DateTime.MinValue;
    public int Rating { get; init; } = 0;
    public int TemperatureC { get; init; } = 0;
    public int rainChance { get; init; } = 0;

}