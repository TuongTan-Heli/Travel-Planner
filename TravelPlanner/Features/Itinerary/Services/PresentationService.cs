using TravelPlanner.Features.Chat.Services;
using System.Text.Json;
using TravelPlanner;
using System.Text.Json.Serialization;

public class PresentationService
{
    private readonly ChatService _chatService;

    public PresentationService(ChatService chatService)
    {
        _chatService = chatService;
    }
    public async Task<FinalPresentation> Present(TravelResponse response, TravelSession session)
    {
        var prompt = PromptBuilder.Build(
                    TravelStage.FinalPresentation,
                    session.Context,
                    response);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        options.Converters.Add(new JsonStringEnumConverter());

        var replyText = await _chatService.GenerateReplyAsync(prompt, session);
        try
        {
            var result = JsonSerializer.Deserialize<FinalPresentation>(
                replyText,
                options) ?? throw new AppException(
                    "INTENT_PARSE_ERROR",
                    "Failed to parse intent extraction result.");

            return result;
        }
        catch (JsonException ex)
        {
            Console.WriteLine(ex.Path);
            Console.WriteLine(ex.LineNumber);
            Console.WriteLine(ex.BytePositionInLine);
            Console.WriteLine(ex.Message);

            throw;
        }

    }
}