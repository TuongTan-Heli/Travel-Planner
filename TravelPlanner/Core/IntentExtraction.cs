using System.Text.Json;
using static TravelPlanner.Utils;

namespace TravelPlanner.Features.Chat.Services;

public sealed class IntentExtractionService
{
    private readonly ChatService _chatService;

    public IntentExtractionService(ChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task<IntentExtractionResponse> ExtractAsync(
        TravelSession session,
        string userMessage)
    {
        var prompt = PromptBuilder.Build(
            TravelStage.IntentExtraction,
            session.Context,
            userMessage);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var replyText = await _chatService.GenerateReplyAsync(prompt);

        var result = JsonSerializer.Deserialize<TravelIntentResult>(
            replyText,
            options);

        if (result is null)
        {
            throw new AppException(
                "INTENT_PARSE_ERROR",
                "Failed to parse intent extraction result.");
        }

        MergeContext(session.Context, result);

        var ready = session.Context.IsReadyForPlanning();

        if (ready)
        {
            session.Stage = TravelStage.LocationSelection;
        }

        return new IntentExtractionResponse
        {
            Message = ready
                ? "Great! I have enough information to start planning your trip."
                : result.AssistantMessage,

            IsReadyForPlanning = ready,

            IntentResult = result
        };
    }

    private static void MergeContext(
        TravelPromptContext ctx,
        TravelIntentResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.Destination))
            ctx.Destination = result.Destination;

        if (result.Days.HasValue)
            ctx.Days = result.Days;

        if (result.Budget.HasValue)
            ctx.Budget = result.Budget;

        if (result.Travelers.HasValue)
            ctx.Travelers = result.Travelers;

        if (result.StartDate != null)
            ctx.StartDate = ParseDate(result.StartDate);

        if (result.EndDate != null)
            ctx.EndDate = ParseDate(result.EndDate);

        if (result.Interests?.Count > 0)
        {
            ctx.Interests = ctx.Interests
                .Union(result.Interests)
                .ToList();
        }

        if (result.Preferences?.Count > 0)
        {
            ctx.Preferences = ctx.Preferences
                .Union(result.Preferences)
                .ToList();
        }
    }
}