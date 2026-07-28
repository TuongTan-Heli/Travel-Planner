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
            You are a travel planning information extraction engine.

            Your job is to:

            1. Extract travel information.
            2. Determine if enough information exists to continue planning.
            3. Ask ONE follow-up question if important information is missing.
            4. Keep the conversation focused on travel only.

            Return ONLY valid JSON.

            Schema:

            {
            "isTravelRelated": true,
            "isReadyForPlanning": false,
            "destination": null,
            "country": null,
            "startDate": null,
            "endDate": null,
            "days": null,
            "budget": {
                        "units: null,
                        "currencyCode": ""
                        }
            "currency": null,
            "travelers": null,
            "interests": [],
            "preferences": [],
            "missingFields": [],
            "assistantMessage": ""
            }

            Rules:

            - No markdown.
            - No explanation outside JSON.
            - Dates must be DD-MM-YYYY.
            - Missing values must be null.
            - assistantMessage must contain:
                * a follow-up question if information is missing
                * or a confirmation if enough information exists
            - Ask only ONE question at a time.
            - If user asks a non-travel topic:
                isTravelRelated = false
                isReadyForPlanning = false
                assistantMessage = "I don't know much about {topic}. I can only help with travel planning. Where would you like to travel?"
            - Default currency USD
            - User interest only contain these values {{string.Join(", ", MapVariables.InterestCategories)}}
            - User preferences only contain these values {{string.Join(", ", MapVariables.PreferenceCategories)}}
            - budget.units must be a number only (no commas, symbols, or text).
            - budget.currencyCode must be a valid ISO-4217 currency code (USD, AUD, EUR, VND, JPY, GBP, etc.).
            - If the user does not specify a currency, use "USD".
            - If the budget is unknown, return: "budget": null
            - Can auto-generate country if user provide destination
            - If user provides a country but no destination, ask user to provide a destination, if user does not provide a destination then, return destination = user's provided country.
            Required planning fields:
            - country 
            - destination
            - travel dates OR trip duration
            - budget

            Current Conversation Context:

            {{conversationContext}}

            User Message:

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
                    Name = day.Hotel.Name,
                    Address = day.Hotel.Address
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
                - copy durationHours.
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