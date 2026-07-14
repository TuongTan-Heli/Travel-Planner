using Microsoft.AspNetCore.Mvc.ModelBinding;
using TravelPlanner.Features.Map.Model;

public class Place
{
    public string Name { get; init; } = string.Empty;

    public string Address { get; init; } = string.Empty;

    public Altitude Location { get; set; } = new();

    public string Country { get; set; } = string.Empty;

    public List<string> Photos { get; set; } = [];

    public string PrimaryType { get; init; } = string.Empty;

    public List<string> Types { get; init; } = [];

    public int Rating { get; init; }

    public int UserRatingCount { get; init; }

    public List<string> OpenTime { get; init; } = [];

    public string PriceLevel { get; init; } = "";

    public PriceRange? PriceRange { get; init; }

    public List<Review> Reviews { get; init; } = [];

    public string ReviewSummary { get; set; } = "";

    public string PhoneNumber { get; init; } = string.Empty;

    public string WebsiteUrl { get; init; } = string.Empty;

    public bool? DineIn { get; init; }

    public bool? AllowsDogs { get; init; }

    public bool? GoodForChildren { get; init; }

    public bool? GoodForGroups { get; init; }

    public bool? GoodForWatchingSports { get; init; }

    public PlaceCategory Category { get; init; }

    public bool? LiveMusic { get; init; }

    public PaymentOptions? PaymentOptions { get; set; }

    public bool? OutdoorSeating { get; init; }

    public bool? Reservable { get; init; }

    public string Description { get; init; } = string.Empty;

    public bool? ServesBeer { get; init; }

    public bool? ServesBreakfast { get; init; }

    public bool? ServesBrunch { get; init; }

    public bool? ServesCocktails { get; init; }

    public bool? ServesCoffee { get; init; }

    public bool? ServesDessert { get; init; }

    public bool? ServesDinner { get; init; }

    public bool? ServesLunch { get; init; }

    public bool? ServesVegetarianFood { get; init; }

    public bool? ServesWine { get; init; }

    public bool? Takeout { get; init; }

    public PlaceScore Score { get; set; } = new();
}
public enum PlaceCategory
{
    Travel,
    Restaurant,
    Hotel,
}
