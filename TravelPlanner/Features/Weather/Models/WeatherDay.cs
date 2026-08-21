public class WeatherDay
{
    public Place Place { get; set; } = new();
    public DateTime Date { get; init; }

    public double MaxTemp { get; init; }

    public double MinTemp { get; init; }

    public double Rainfall { get; init; }

    public int WeatherCode { get; init; }

    public double AvgTemp =>
        (MaxTemp + MinTemp) / 2;

    public int Score { get; init; }
}