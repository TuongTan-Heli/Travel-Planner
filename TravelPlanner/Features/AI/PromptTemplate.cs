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
            },

            Itinerary = response.Itinerary.DayPlans.Select(day => new
            {
                day.DayNumber,
                Hotel = day.Hotel?.Name,

                Activities = day.Stops.Select(stop => new
                {
                    PlaceName = stop.Place.Name,
                    Category = stop.Place.Category.ToString(),
                    PrimaryType = stop.Place.PrimaryType,
                    Address = stop.Place.Address,
                    Score = stop.Place.Score.TotalScore,
                    Type = stop.Type.ToString(),
                    Duration = stop.EstimatedHours
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
        })
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
            "generalTips": [
                "string"
            ],
            "days": [
                {
                "dayNumber": "int",
                "summary": "string",
                "weather": "string|null",
                "tips": [
                    "string"
                ],
                "activities": [
                    {
                    "placeId": "string",
                    "placeName": "string",
                    "type": "Breakfast | Attraction | Lunch | Coffee | Dinner | FreeTime",
                    "description": "Explain what this place offers.",
                    "whyVisit": "Explain why this stop was selected for this itinerary.",
                    "alternatives": [
                        {
                        "placeId": "string",
                        "placeName": "string",
                        "whyVisit": "Why this is a good alternative.",
                        }
                    ]
                    }
                ]
                }
            ]
            }

            Rules:

            - Generate a concise Trip Summary (2-3 sentences).
            - Generate 3-5 General Tips for the whole trip.
            - Generate a short summary for each day.
            - Generate a short description for every activity.
            - Explain why each activity is worth visiting.
            - If weather information exists, summarize it in one sentence.
            - Keep descriptions concise (1-3 sentences).
            - Use a friendly travel-guide tone.
            - Never invent places.
            - Never recommend places outside the provided planning result.
            - Alternatives MUST come ONLY from the provided candidate places.
            - Prefer alternatives in the same cluster/area.
            - Prefer alternatives with similar experience or category.
            - Do not repeat places already scheduled unless there are no other suitable alternatives.
            - Return ONLY valid JSON.
            - Type: only use one of these values: Breakfast | Attraction | Lunch | Coffee | Dinner | FreeTime",

            Trip information

            {{JsonSerializer.Serialize(promptData)}}
            """;
    }
}