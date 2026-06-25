public class Place
{
    public string Name { get; init; } = string.Empty;

    public string Address { get; init; } = string.Empty;

    public Altitude? Location { get; set; }

    public List<string> Photos { get; set; } = [];

    public string PrimaryType { get; init; } = string.Empty;

    public List<string> Types { get; init; } = [];

    public int Rating { get; init; }

    public int UserRatingCount { get; init; }

    public List<string> OpenTime { get; init; } = [];

    public int PriceLevel { get; init; }

    public string PriceRange { get; init; } = string.Empty;

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
}
public enum PlaceCategory
{
    Travel,
    Restaurant,
    Hotel,
}
public class Review
{
    public int Rating { get; init; }
    public string Text { get; init; } = string.Empty;
}

public class ReviewSummary
{
    public string Text { get; set; } = string.Empty;
}

public class Photo
{
    public string Name { get; set; } = string.Empty;
}

public class PaymentOptions
{
    public bool AcceptsCreditCards { get; set; }
    public bool AcceptsDebitCards { get; set; }
    public bool AcceptsCashOnly { get; set; }
    public bool? AcceptsNfc { get; set; }

}