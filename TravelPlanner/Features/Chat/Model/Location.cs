public sealed class Location
{
    public List<Altitude> Results { get; set; } = [];
}

public sealed class Altitude
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}