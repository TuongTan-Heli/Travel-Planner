public class Itinerary
{
    public List<DayPlan> DayPlans { get; set; } = [];
}

public class DayPlan
{
    public Place Hotel { get; set; } = new();
    public int DayNumber { get; set; }
    public List<ItineraryStop> Stops { get; set; } = [];
    public double TotalHours { get; set; }
}

public class ItineraryStop
{
    public Place Place { get; set; } = new();

    public StopType Type { get; set; }

    public double EstimatedHours { get; set; }

    public double TravelMinutesFromPrevious { get; set; }
}

public enum StopType
{
    Breakfast,
    Attraction,
    Lunch,
    Coffee,
    Dinner,
    FreeTime
}