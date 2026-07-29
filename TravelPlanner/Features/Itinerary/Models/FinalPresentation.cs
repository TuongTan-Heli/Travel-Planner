using TravelPlanner.Features.AI.Model;
using TravelPlanner.Features.Map.Model;

public class FinalPresentation
{
    public string TripSummary { get; init; } = "";

    public List<string> GeneralTips { get; init; } = [];

    public PresentationTrip Trip { get; init; } = new();

    public List<PresentationDay> Itinerary { get; init; } = [];

    public TravelTimePresentation? TravelTime { get; init; }

    public List<PlacePresentation> CandidatePlaces { get; set; } = [];
}

public class PresentationTrip
{
    public string? Country { get; init; }

    public string? Destination { get; init; }

    public string? StartDate { get; init; }

    public string? EndDate { get; init; }

    public int? Days { get; init; }

    public object? Budget { get; init; }

    public int? Travelers { get; init; }

    public List<string> Interests { get; init; } = [];

    public List<string> Preferences { get; init; } = [];
}

public class PresentationDay
{
    public int DayNumber { get; init; }

    public string Summary { get; init; } = "";

    public string? Weather { get; init; }

    public List<string> Tips { get; init; } = [];

    public PresentationActivity? Hotel { get; set; }

    public List<PresentationActivity> Activities { get; init; } = [];
}

public class PresentationActivity
{
    public StopType Type { get; init; }

    public string Description { get; init; } = "";

    public string WhyVisit { get; init; } = "";

    public string StopType { get; init; } = "";

    public double DurationHours { get; init; }

    public int TravelMinutesFromPrevious { get; init; }

    public PlacePresentation Place { get; set; } = new();

    public List<PlacePresentation> Alternatives { get; init; } = [];
}

public class PlacePresentation
{
    public string PlaceId { get; init; } = "";
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string PrimaryType { get; set; } = "";
    public string Category { get; set; } = "";
    public double? Rating { get; set; }

    public LocationPresentation Location { get; set; } = new();

    public List<string> Types { get; set; } = [];

    public PriceRange? PriceRange { get; set; }

    public List<Review> Reviews { get; set; } = [];

    public string ReviewSummary { get; set; } = "";

    public List<string> OpenTime { get; set; } = [];

    public string PhoneNumber { get; set; } = "";

    public string WebsiteUrl { get; set; } = "";

    public bool? DineIn { get; set; }

    public bool? AllowsDogs { get; set; }

    public bool? GoodForChildren { get; set; }

    public bool? GoodForGroups { get; set; }

    public bool? GoodForWatchingSports { get; set; }

    public bool? LiveMusic { get; set; }

    public PaymentOptions? PaymentOptions { get; set; }

    public bool? OutdoorSeating { get; set; }

    public bool? Reservable { get; set; }

    public string Description { get; set; } = "";

    public bool? ServesBeer { get; set; }

    public bool? ServesBreakfast { get; set; }

    public bool? ServesCocktails { get; set; }

    public bool? ServesLunch { get; set; }

    public bool? ServesDinner { get; set; }

    public bool? ServesBrunch { get; set; }

    public bool? ServesCoffee { get; set; }

    public bool? ServesDessert { get; set; }

    public int? UserRatingCount { get; set; }

    public string? PriceLevel { get; set; }

    public bool? Takeout { get; set; }
}


public class LocationPresentation
{
    public double Latitude { get; init; }

    public double Longitude { get; init; }
}

public class TravelTimePresentation
{
    public string? StartTime { get; init; }

    public string? EndTime { get; init; }

    public double? WeatherScore { get; init; }

    public List<ForecastPresentation> Forecasts { get; init; } = [];
}

public class ForecastPresentation
{
    public LocationPresentation Location { get; init; } = new();

    public List<WeatherDayPresentation> Days { get; init; } = [];
}

public class WeatherDayPresentation
{
    public string Date { get; init; } = "";

    public double AvgTemp { get; init; }

    public double MaxTemp { get; init; }

    public double MinTemp { get; init; }

    public double Rainfall { get; init; }

    public string WeatherCode { get; init; } = "";

    public double Score { get; init; }
}