using System.Text.Json;
using TravelPlanner.Features.Chat.Services;

public class IntentExtraction
{

    private readonly ChatService _chatService;
    public IntentExtraction(ChatService chatService)
    {
        _chatService = chatService;
    }
    // private async Task<TravelWorkflowResult> HandleIntentExtraction(
    // TravelSession session,
    // string userMessage)
    // {
    //     var prompt =
    //         PromptBuilder.Build(
    //             TravelStage.IntentExtraction,
    //             session.Context,
    //             userMessage);

    //     var aiJson =
    //         await _chatService.GenerateReplyAsync(prompt);

    //     var result =
    //         JsonSerializer.Deserialize<TravelIntentResult>(
    //             aiJson);

    //     if (result == null)
    //     {
    //         throw new AppException(
    //             "INTENT_PARSE",
    //             "Failed to parse AI response");
    //     }

    //     MergeContext(
    //         session.Context,
    //         result);

    //     if (!result.IsTravelRelated)
    //     {
    //         return new TravelWorkflowResult
    //         {
    //             Message = result.AssistantMessage
    //         };
    //     }

    //     if (!session.Context.IsReadyForPlanning())
    //     {
    //         return new TravelWorkflowResult
    //         {
    //             Message = result.AssistantMessage
    //         };
    //     }

    //     session.Stage =
    //         TravelStage.LocationSelection;

    //     return new TravelWorkflowResult
    //     {
    //         IsReadyForPlanning = true,
    //         NextAction = TravelStage.LocationSelection,
    //         Message =
    //             "Great! I have enough information to start building your trip."
    //     };
    // }
}