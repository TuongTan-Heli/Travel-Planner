export type ActivityType =
    | 'Breakfast'
    | 'Attraction'
    | 'Lunch'
    | 'Coffee'
    | 'Dinner'
    | 'FreeTime';


export interface Itinerary {
    tripSummary: string;
    generalTips: string[];

    trip: Trip;

    itinerary: Day[];

    travelTime?: TravelTime | null;

    candidatePlaces: Place[];
}


export interface Trip {
    country?: string | null;
    destination?: string | null;
    startDate?: string | null;
    endDate?: string | null;
    days?: number | null;
    budget?: unknown | null;
    travelers?: number | null;
    interests: string[];
    preferences: string[];
}


export interface Day {
    dayNumber: number;
    summary: string;
    weather: string | null;
    tips: string[];

    hotel: Activity | null;

    activities: Activity[];
}


export interface Activity {
    type: ActivityType;

    description: string;

    whyVisit: string;

    stopType: string;

    durationHours: number;

    travelMinutesFromPrevious: number;

    place: Place;

    alternatives: Place[];
}


// export interface Alternative {
//     whyVisit: string;

//     place: Place;
// }


export interface Place {
    placeId: string;

    name: string;

    address: string;

    primaryType: string;

    category: string;

    rating?: number | null;


    location: Location;


    // Extra details
    types: string[];

    priceRange?: PriceRange | null;

    reviews: Review[];

    reviewSummary: string;

    openTime: string[];

    phoneNumber: string;

    websiteUrl: string;


    // Amenities
    dineIn?: boolean | null;

    allowsDogs?: boolean | null;

    goodForChildren?: boolean | null;

    goodForGroups?: boolean | null;

    goodForWatchingSports?: boolean | null;

    liveMusic?: boolean | null;

    outdoorSeating?: boolean | null;

    reservable?: boolean | null;


    servesBeer?: boolean | null;

    servesBreakfast?: boolean | null;

    servesCocktails?: boolean | null;

    servesLunch?: boolean | null;

    servesDinner?: boolean | null;

    servesBrunch?: boolean | null;

    servesCoffee?: boolean | null;

    servesDessert?: boolean | null;


    takeout?: boolean | null;


    // Payment
    paymentOptions?: PaymentOptions | null;


    // Other
    description: string;

    userRatingCount?: number | null;

    priceLevel?: string | null;
}


export interface PaymentOptions {
    acceptsCreditCards?: boolean | null;

    acceptsDebitCards?: boolean | null;

    acceptsCashOnly?: boolean | null;

    acceptsNfc?: boolean | null;
}


export interface Review {
    text: string;

    rating: number;
}


export interface Location {
    latitude: number;

    longitude: number;
}


export interface PriceRange {
    startPrice?: PriceValue | null;

    endPrice?: PriceValue | null;
}


export interface PriceValue {
    units: number;

    currencyCode: string;
}


export interface TravelTime {
    startTime?: string | null;

    endTime?: string | null;

    weatherScore?: number | null;

    forecasts: Forecast[];
}


export interface Forecast {
    location: Location;

    days: WeatherDay[];
}


export interface WeatherDay {
    date: string;

    avgTemp: number;

    maxTemp: number;

    minTemp: number;

    rainfall: number;

    weatherCode: string;

    score: number;
}

export interface SelectedStop {
    place: Place;
    label?: string;
    dayNumber?: number;
    stop: Activity;
}