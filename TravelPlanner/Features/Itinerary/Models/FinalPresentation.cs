using TravelPlanner.Features.AI.Model;

public class FinalPresentation
{
    public string TripSummary { get; init; } = "";

    public List<string> GeneralTips { get; init; } = [];

    public List<PresentationDay> Days { get; init; } = [];
}

public class PresentationDay
{
    public int DayNumber { get; init; }

    public string Summary { get; init; } = "";

    public string? Weather { get; init; }

    public List<PresentationActivity> Activities { get; init; } = [];

    public List<string> Tips { get; init; } = [];
}

public class PresentationActivity
{
    public string PlaceId { get; init; } = "";

    public string PlaceName { get; init; } = "";

    public StopType Type { get; init; }

    public string Description { get; init; } = "";

    public string WhyVisit { get; init; } = "";

    public List<ActivityAlternative> Alternatives { get; init; } = [];
}

public class ActivityAlternative
{
    public string PlaceId { get; init; } = "";

    public string PlaceName { get; init; } = "";

    public string WhyVisit { get; init; } = "";
}