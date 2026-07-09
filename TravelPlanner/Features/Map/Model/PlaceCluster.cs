public class PlaceCluster
{
    public Altitude Center { get; set; } = new();

    public List<Place> Places { get; set; } = [];

    public string CityName { get; set; } = "";

    public double Score { get; set; }

    public Place RepresentativePlace { get; set; } = new();
}