public class PlaceScore
{
    public double TotalScore { get; set; }

    public Dictionary<string, double> Breakdown { get; init; } = [];
}

public class ScoreWeights
{
    public double Rating = 25;
    public double Budget = 20;
    public double Interest = 50;
    public double Route = 25;
    public double Local = 40;
    public double MaxScore => Rating + Budget + Interest + Route + Local;
}