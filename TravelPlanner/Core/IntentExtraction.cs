using System.Text.Json;
using System.Text.Json.Serialization;

namespace TravelPlanner.Features.Chat.Services;

public sealed class IntentExtractionService
{
    private readonly ChatService _chatService;
    private readonly Utils _utils;

    public IntentExtractionService(ChatService chatService, Utils utils)
    {
        _chatService = chatService;
        _utils = utils;
    }

    public async Task<IntentExtractionResponse> ExtractAsync(
        TravelSession session,
        TravelResponse response,
        string userMessage)
    {
        var prompt = PromptBuilder.Build(
            TravelStage.IntentExtraction,
            session.Context,
            response,
            userMessage);

        var replyText = await _chatService.GenerateReplyAsync(prompt, session);

        var result = JsonSerializer.Deserialize<TravelIntentResult>(
            replyText,
            new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            }) ?? throw new AppException(
                "INTENT_PARSE_ERROR",
                "Failed to parse intent extraction result.",
                "Failed to parse intent extraction result.");
                
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

    private void MergeContext(
        TravelPromptContext ctx,
        TravelIntentResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.Destination))
            ctx.Destination = result.Destination;

        if (!string.IsNullOrWhiteSpace(result.Country))
            ctx.Country = result.Country;

        if (result.Days.HasValue)
            ctx.Days = result.Days;

        if (result.Budget is not null)
            ctx.Budget = result.Budget;

        if (result.Travelers.HasValue)
            ctx.Travelers = result.Travelers;

        if (result.StartDate != null)
            ctx.StartDate = _utils.ParseDate(result.StartDate);

        if (result.EndDate != null)
            ctx.EndDate = _utils.ParseDate(result.EndDate);

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
        
        if (result.MinRating.HasValue)
        {
            ctx.MinRating = result.MinRating;
        }

        if (result.TravelFrequency.HasValue)
        {
            ctx.TravelFrequency = result.TravelFrequency;
        }
    }
}