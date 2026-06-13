using TravelPlanner.Features.Chat.Services;

public class TravelPlannerWorkflow
{
    private readonly ChatService _chatService;

    public TravelPlannerWorkflow(
        ChatService chatService)
    {
        _chatService = chatService;
    }

    // public async Task<TravelWorkflowResult> ProcessAsync(
    //     TravelSession session,
    //     string userMessage)
    // // {
    //     switch (session.Stage)
    //     {
    //         case TravelStage.IntentExtraction:
    //             return await HandleIntentExtraction(
    //                 session,
    //                 userMessage);

    //         default:
    //             return new TravelWorkflowResult
    //             {
    //                 Message = "Unsupported stage."
    //             };
    //     }
    // }
}