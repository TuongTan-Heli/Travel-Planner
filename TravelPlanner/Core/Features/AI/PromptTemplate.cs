using System.Text.Json;
using TravelPlanner.Features.Map.Model;
public class PromptTemplate
{
    public static string BuildIntentExtractionPrompt(
    string userMessage,
    string conversationContext = ""
    )
    {

        return $$"""
            You are a travel-planning information extraction engine.

            Extract travel requirements from the conversation and latest user message.
            Preserve previously provided information. Do not invent information.

            Return ONLY valid JSON matching this schema:

            {
            "isTravelRelated": true,
            "isReadyForPlanning": false,
            "destination": null,
            "country": null,
            "startDate": null,
            "endDate": null,
            "days": null,
            "budget": {
                "units": 0,
                "currencyCode": "AUD"
            },
            "travelers": null,
            "interests": [],
            "preferences": [],
            "minRating": null,
            "assistantMessage": "",
            "travelFrequency": null
            }

            RULES:

            1. Travel
            - If the message is not travel-related:
            - isTravelRelated = false
            - isReadyForPlanning = false
            - assistantMessage = "I can only help with travel planning. Where would you like to travel?"
            - Otherwise isTravelRelated = true.

            2. Planning readiness
            Planning is ready ONLY when all are known:
            - destination
            - country
            - budget
            - either days OR both startDate and endDate

            Set isReadyForPlanning accordingly.

            3. Destination / country
            - Infer country when the destination clearly identifies it.
            - If only a country is provided, ask for a destination unless the user clearly intends the country itself as the destination.
            - Never invent a destination.

            4. Dates
            - Format dates as DD-MM-YYYY.
            - days may be provided instead of dates.
            - If both startDate and endDate are known, calculate inclusive days:
            days = endDate - startDate + 1
            - If only one date is provided, ask for the missing date.
            - If days is provided, do not ask for dates.
            - If both dates and days are provided but conflict, use the dates to calculate days.
            - Resolve unambiguous relative dates when possible.

            5. Budget
            - budget.units must be numeric only.
            - budget.currencyCode must be ISO-4217.
            - If currency is omitted, use AUD.
            - If budget is unknown, set budget = null.
            - Never infer a budget.

            6. Optional fields
            - travelers: extract only when provided.
            - minRating: extract only when explicitly provided.
            - interests must ONLY use:
            {{string.Join(", ", MapVariables.InterestCategories)}}
            - preferences must ONLY use:
            {{string.Join(", ", MapVariables.PreferenceCategories)}}
            - travelFrequency must ONLY use:
            {{string.Join(", ", Enum.GetNames(typeof(TravelFrequency)))}}

            7. Conversation
            - Use the entire conversation context.
            - Do not ask for information already provided.
            - Ask at most ONE question.
            - Never ask user the same question twice.
            - If required information is missing, ask for the highest-priority missing field in this order:
            destination → country → dates/days → budget.
            - If all required information exists, provide a short confirmation in assistantMessage.
            - Missing scalar values = null.
            - Missing arrays = [].

            CURRENT CONVERSATION:
            {{conversationContext}}

            LATEST USER MESSAGE:
            {{userMessage}}
            """;
    }
    public static string BuildLocationSelectionPrompt(TravelPromptContext context)
    {
        throw new NotImplementedException();
    }

    public static string BuildItineraryGenerationPrompt(TravelPromptContext context)
    {
        throw new NotImplementedException();
    }

    public static string BuildFinalPresentationPrompt(
    TravelPromptContext context,
    TravelResponse response)
    {
        if (response is null || context is null)
        {
            throw new AppException(
                "PRMT_BUI_FAIL",
                "Something went wrong while building the prompt template.",
                "Failed to build prompt template");
        }

        var promptData = new
        {
            Trip = new
            {
                context.Country,
                context.Destination,
                context.StartDate,
                context.EndDate,
                context.Days,
                context.Budget,
                context.Travelers,
                context.Interests,
                // Preferences = context.Preferences
            },

            Itinerary = response.Itinerary.DayPlans.Select(day => new
            {
                day.DayNumber,
                Hotel = day.Hotel == null ? null : new
                {
                    PlaceId = BuildPlaceId(day.Hotel),
                    PlaceName = day.Hotel.Name,
                    Address = day.Hotel.Address,
                    PrimaryType = day.Hotel.PrimaryType
                },

                Activities = day.Stops.Select(stop => new
                {
                    PlaceId = BuildPlaceId(stop.Place),

                    Place = new
                    {
                        Name = stop.Place.Name,
                        Address = stop.Place.Address,
                        Category = stop.Place.Category.ToString(),
                        PrimaryType = stop.Place.PrimaryType,
                        Rating = stop.Place.Rating,
                        Location = stop.Place.Location
                    },

                    Type = stop.Type.ToString(),
                    DurationHours = stop.EstimatedHours,
                    TravelMinutesFromPrevious = stop.TravelMinutesFromPrevious
                })
            }),

            CandidatePlaces = response.TripPlanningData.RecommendedPlaces
            .Select(p => new
            {
                p.Name,
                p.Category,
                p.PrimaryType,
                p.Address,
                Score = p.Score.TotalScore
            }),
            TravelTime = response.TripPlanningData.TravelTime == null ? null : new
            {
                response.TripPlanningData.TravelTime.StartTime,
                response.TripPlanningData.TravelTime.EndTime,
                response.TripPlanningData.TravelTime.WeatherScore,
                Forecasts = response.TripPlanningData.TravelTime.Forecasts.Select(x => new
                {
                    Location = new
                    {
                        x.Location.Latitude,
                        x.Location.Longitude
                    },
                    Days = x.Days.Select(d => new
                    {
                        d.Date,
                        d.AvgTemp,
                        d.MaxTemp,
                        d.MinTemp,
                        d.Rainfall,
                        d.WeatherCode,
                        d.Score
                    })
                })
            },
        };

        return $$"""
            You are an experienced travel guide.

            Your task is to PRESENT the generated itinerary.
            DO NOT redesign the itinerary.
            DO NOT change the activity order.
            DO NOT invent hotels, restaurants or attractions.
            ONLY use places provided in the planning result.

            Return ONLY valid JSON.
            Do not wrap the JSON in markdown.
            
            JSON Schema:

            {
                "tripSummary": "string",
                "generalTips": ["string"],

                "trip": {
                    "country": "string|null",
                    "destination": "string|null",
                    "startDate": "string|null",
                    "endDate": "string|null",
                    "days": "number|null",
                    "budget": "object|null",
                    "travelers": "number|null",
                    "interests": ["string"]
                },

                "itinerary": [
                    {
                    "dayNumber": "number",
                    "summary": "string",
                    "weather": "string|null",
                    "tips": ["string"],

                    "hotel": {
                        "type": "Breakfast | Attraction | Lunch | Coffee | Dinner | FreeTime | Hotel",

                        "description": "string",
                        "whyVisit": "string",

                        "stopType": "string",
                        "durationHours": "number",
                        "travelMinutesFromPrevious": "number",

                        "place": {
                            "placeId": "string",
                            "name": "string",
                            "address": "string",
                            "primaryType": "string",
                            "category": "string",
                            "rating": "number|null",
                            "location": {
                            "latitude": "number",
                            "longitude": "number"
                            }
                        },
                        
                    },

                    "activities": [
                        {
                        "type": "Breakfast | Attraction | Lunch | Coffee | Dinner | FreeTime",

                        "description": "string",
                        "whyVisit": "string",

                        "stopType": "string",
                        "durationHours": "number",
                        "travelMinutesFromPrevious": "number",

                        "place": {
                            "placeId": "string",
                            "name": "string",
                            "address": "string",
                            "primaryType": "string",
                            "category": "string",
                            "rating": "number|null",
                            "location": {
                            "latitude": "number",
                            "longitude": "number"
                            }
                        },
                        }
                    ]
                    }
                ],

                "travelTime": {
                    "startTime": "string|null",
                    "endTime": "string|null",
                    "weatherScore": "number|null",
                    "forecasts": []
                }
                }

            Generate:
                Presentation:
                - tripSummary
                - generalTips

                Trip:
                - copy trip information exactly.

                Itinerary:
                - copy every dayNumber.
                - copy every hotel.
                - copy every activity place.
                - copy stopType.
                - copy durationHours, if null -> 0
                - copy travelMinutesFromPrevious.

                Only generate:
                - summary
                - weather
                - tips
                - description
                - whyVisit

                TravelTime:
                - copy from source if available.
                - otherwise return null.

                Trip information must always be returned.
                Copy exactly from source.
                Never omit this field.
                placeId must be copied exactly from source.
                Do not generate placeId.
                Trip information

            {{JsonSerializer.Serialize(promptData)}}
            """;
    }

    private static string BuildPlaceId(Place place)
    {
        return $"{place.Name}|{place.Address}|{place.Location.Latitude}|{place.Location.Longitude}";
    }
}