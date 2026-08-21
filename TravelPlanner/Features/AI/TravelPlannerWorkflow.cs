using TravelPlanner.Features.Chat.Services;

public class TravelPlannerWorkflow
{
    private readonly ChatService _chatService;

    public TravelPlannerWorkflow(
        ChatService chatService)
    {
        _chatService = chatService;
    }
}