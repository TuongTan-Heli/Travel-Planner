namespace TravelPlanner.Features.Map.Model;

public class GooglePlacesResponse
{
    public List<GooglePlace> Places { get; set; } = new();
}

public class GooglePlace
{
    public DisplayName DisplayName { get; set; } = new();

    public string? FormattedAddress { get; set; }

    public Altitude? Location { get; set; }

    public List<Photo>? Photos { get; set; }

    public string PrimaryType { get; set; } = "";

    public List<string>? Types { get; set; }

    public double? Rating { get; set; }

    public int? UserRatingCount { get; set; }

    public CurrentOpeningHours? CurrentOpeningHours { get; set; }

    public int? PriceLevel { get; set; }

    public PriceRange? PriceRange { get; set; }

    public List<GoogleReview>? Reviews { get; set; }

    public ReviewSummary? ReviewSummary { get; set; }

    public string? InternationalPhoneNumber { get; set; }

    public string? WebsiteUri { get; set; }
    public bool? DineIn { get; set; }

    public bool? AllowsDogs { get; set; }

    public bool? GoodForChildren { get; set; }

    public bool? GoodForGroups { get; set; }

    public bool? GoodForWatchingSports { get; set; }

    public bool? LiveMusic { get; set; }

    public PaymentOptions? PaymentOptions { get; set; }

    public bool? OutdoorSeating { get; set; }

    public bool? Reservable { get; set; }

    public EditorialSummary? EditorialSummary { get; set; }

    public bool? ServesBeer { get; set; }

    public bool? ServesBreakfast { get; set; }

    public bool? ServesBrunch { get; set; }

    public bool? ServesCocktails { get; set; }

    public bool? ServesCoffee { get; set; }

    public bool? ServesDessert { get; set; }

    public bool? ServesDinner { get; set; }

    public bool? ServesLunch { get; set; }

    public bool? ServesVegetarianFood { get; set; }

    public bool? ServesWine { get; set; }

    public bool? Takeout { get; set; }
}

public class Review
{
    public string Text { get; set; } = string.Empty;

    public double Rating { get; set; }
}
public class DisplayName
{
    public string Text { get; set; } = string.Empty;
    public string? LanguageCode { get; set; }
}

public class CurrentOpeningHours
{
    public List<string> WeekDayDescriptions { get; set; } = [];
}

public class EditorialSummary
{
    public string Text { get; set; } = string.Empty;
}

public class GoogleReview
{
    public int Rating { get; set; }
    public ReviewText Text { get; set; } = new();
}

public class ReviewText
{
    public string Text { get; set; } = string.Empty;
    public string? LanguageCode { get; set; }
}
public class Photo
{
    public string Name { get; set; } = "";
}

public class PriceRange
{
    public Money? StartPrice { get; set; }

    public Money? EndPrice { get; set; }
}

public class Money
{
    public string? CurrencyCode { get; set; }

    public string? Units { get; set; }

    public int? Nanos { get; set; }
}

public class ReviewSummary
{
    public string? Text { get; set; }
}