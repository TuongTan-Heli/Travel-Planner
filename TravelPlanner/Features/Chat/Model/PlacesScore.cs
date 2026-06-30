public class PlaceScore
{
    public double TotalScore { get; set; }

    public Dictionary<string, double> Breakdown { get; init; } = [];
}

public class ScoreWeights
{
    public double Rating = 30;

    public double Budget = 15;

    public double Interest = 70;

    public double Route = 30;

    public double Crowd = 10;
}