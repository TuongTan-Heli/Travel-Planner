// Error handling and logging utilities
using System.Net.WebSockets;
using System.Text.Json;
using TravelPlanner;

public class ErrorHandlerService
{
    private readonly ILogger<ErrorHandlerService> _logger;
    private readonly TravelHistoryService _travelHistoryService;

    private readonly Utils _utils;

    public ErrorHandlerService(
        ILogger<ErrorHandlerService> logger,
        TravelHistoryService travelHistoryService,
        Utils utils)
    {
        _logger = logger;
        _travelHistoryService = travelHistoryService;
        _utils = utils;
    }

    public async Task<ErrorPayload> HandleErrorAsync(
        TravelSession session,
        AppException exception,
        string path)
    {
        var correlationId = Guid.NewGuid().ToString();

        await _travelHistoryService.SaveFailureAsync(
            session,
            exception.Code,
            $"{exception.Message} {path}");

        _logger.LogError(
            exception,
            "Application Error. Code={Code}. Message={Message}. Path={Path}. CorrelationId={CorrelationId}",
            exception.Code,
            exception.Message,
            path,
            correlationId);

        session.Reset();

        return new ErrorPayload
        {
            Type = "error",
            Code = exception.Code,
            Message = exception.DisplayMessage,
            CorrelationId = correlationId
        };

    }

    public async Task HandleWebSocketErrorAsync(
        WebSocket socket,
        TravelSession session,
        AppException exception)
    {
        var payload = await HandleErrorAsync(
            session,
            exception,
            "/ws/chat");

        if (socket.State != WebSocketState.Open)
            return;

        var json = JsonSerializer.SerializeToUtf8Bytes(payload);

        await socket.SendAsync(
            new ArraySegment<byte>(json),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);


        await _utils.BroadcastAsync(
            socket,
            new ErrorMessage
            {
                Type = WebSocketMessType.Error,
                Code = exception.Code,
                DisplayMessage = exception.DisplayMessage
            });

        // Tell frontend that the conversation can start again
        await _utils.BroadcastStateAsync(socket, false, "");
    }

    public class ErrorPayload
    {
        public string Type { get; init; } = "error";
        public string Code { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public string CorrelationId { get; init; } = string.Empty;
    }
}