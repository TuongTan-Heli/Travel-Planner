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

    public static string BuildRequirementPrompt(
    TravelPromptContext ctx)
    {
        return $$"""
        You are a travel planner.

        Current trip information:

        Destination: {{ctx.Destination}}
        Days: {{ctx.Days}}
        Budget: {{ctx.Budget}}
        Travelers: {{ctx.Travelers}}

        Ask ONE concise travel question to collect the most important missing information.

        Rules:
        - Ask only one question.
        - Travel related only.
        - Friendly tone.
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

    public static string BuildFinalPresentationPrompt(TravelPromptContext context)
    {
        throw new NotImplementedException();
    }
}