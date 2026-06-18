namespace TravelPlanner.Features.Map.Model;

public class GooglePlacesResponse
{
    public List<GooglePlace> Places { get; set; } = new();
}

public class GooglePlace
{
    public DisplayName DisplayName { get; set; } = new();

    public string? FormattedAddress { get; set; }

    public double? Rating { get; set; }

    public int? PriceLevel { get; set; }

    public List<string>? Types { get; set; }

    public EditorialSummary? EditorialSummary { get; set; }

    public List<GoogleReview>? Reviews { get; set; }

    public CurrentOpeningHours? CurrentOpeningHours { get; set; }
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
    public bool OpenNow { get; set; }
}

public class EditorialSummary
{
    public string Text { get; set; } = string.Empty;
}

public class GoogleReview
{
    public string Name { get; set; } = string.Empty;

    public ReviewText Text { get; set; } = new();
}

public class ReviewText
{
    public string Text { get; set; } = string.Empty;
    public string? LanguageCode { get; set; }
}