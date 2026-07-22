export interface Itinerary {
    GeneralTips: string[];
    TripSummary: string;
    Days: Day[];
}

interface Day {
    DayNumber: number;
    Summary: string;
    Tips: string[];
    Weather: Weather;
    Activities: Activity[];
}

interface Weather {
    Temperature: string;
    Condition: string;
    Wind: string;
    Humidity: string;
}

interface Activity {
    PlaceName: string;
    Description: string;
    PlaceId: string;
    Type: string;
    WhyVisit: string;
    Alternatives: Alternative[];
}

interface Alternative {
    PlaceName: string;
    PlaceId: string;
    WhyVisit: string;
}