using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TravelPlanner.Features.Chat.Models;

namespace TravelPlanner.Features.Chat.Services;

public sealed class ChatWebSocketService
{
    private readonly ConcurrentDictionary<WebSocket, object> _sockets = new();
    private readonly IntentExtractionService _intentExtractionService;
    private readonly ConcurrentDictionary<WebSocket, TravelSession> _sessions = new();
    private readonly ChatService _chatService;
    private readonly Utils _utils;
    private readonly Planner _planner;
    private readonly ILogger<ChatWebSocketService> _logger;

    private static readonly JsonSerializerOptions JsonOptions =
    new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };
    public ChatWebSocketService(IntentExtractionService intentExtractionService, ChatService chatService, Utils utils, Planner planner, ILogger<ChatWebSocketService> logger)
    {
        _intentExtractionService = intentExtractionService;
        _chatService = chatService;
        _utils = utils;
        _planner = planner;
        _logger = logger;
    }
    private TravelSession GetSession(WebSocket socket)
    {
        return _sessions.GetOrAdd(socket, _ => new TravelSession());
    }

    #region Handle WebSocket Connections
    public async Task HandleAsync(WebSocket socket)
    {
        _sockets.TryAdd(socket, new object());
        _sessions.TryAdd(socket, new TravelSession());

        try
        {
            // await SendHistoryAsync(socket);
            await ReceiveLoopAsync(socket);
        }
        finally
        {
            _sessions.TryRemove(socket, out var session);
            _sockets.TryRemove(socket, out _);
            _chatService.ClearSessionHistory(session);
            await CloseSocketAsync(socket);
        }
    }
    private async Task HandleChatMessage(
    WebSocket socket,
    string messageJson)
    {
        var input =
            JsonSerializer.Deserialize<ChatMessageInput>(
                messageJson,
                JsonOptions)
            ?? throw new AppException(
                "WS_INVALID_CHAT",
                "Cannot deserialize chat message, please try again.",
                "Invalid chat message.");

        if (string.IsNullOrWhiteSpace(input.Text))
            throw new AppException(
                "WS_EMPTY",
                "Message cannot be empty.",
                "Chat message cannot be empty.");

        var session = GetSession(socket);

        var response = new TravelResponse();

        await BroadcastUserMessage(socket, input);

        if (session.Stage == TravelStage.IntentExtraction)
        {
            await ExecuteIntentExtraction(
                socket,
                session,
                response,
                input.Text);
        }

        await _planner.ContinuePlanningAsync(
            socket,
            session,
            response);
    }
    private async Task HandlePlannerMessage(
    WebSocket socket,
    string messageJson)
    {
        var envelope = JsonSerializer.Deserialize<MessageEnvelope>(messageJson, JsonOptions)
        ?? throw new AppException(
            "WS_INVALID",
            "Invalid message",
            "Received invalid message format.");

        var request = envelope.Data.Deserialize<PlannerRequest>(JsonOptions)
        ?? throw new AppException(
            "WS_INVALID_PLANNER",
            "Invalid planner request",
            "Received invalid planner request format.");

        var session = GetSession(socket);

        request.ApplyTo(session);

        session.Stage = TravelStage.LocationSelection;

        await _planner.ContinuePlanningAsync(
            socket,
            session,
            new TravelResponse());
    }
    private async Task ReceiveLoopAsync(WebSocket socket)
    {
        var buffer = new byte[4096];
        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                var count = result.Count;
                while (!result.EndOfMessage)
                {
                    if (count >= buffer.Length)
                    {
                        throw new AppException("WS_MSG_TOO_LARGE", "Message too large.", "WebSocket message too large.");
                    }

                    result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer, count, buffer.Length - count), CancellationToken.None);
                    count += result.Count;
                }

                if (count == 0)
                {
                    continue;
                }

                var messageJson = Encoding.UTF8.GetString(buffer, 0, count);
                await HandleIncomingMessageAsync(socket, messageJson);
            }
        }
        catch (AppException ex)
        {
            await HandleWebSocketErrorAsync(socket, ex);
        }
        catch (WebSocketException ex)
        {
            // Connection-level WebSocket error
            Console.WriteLine($"WebSocket error: {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            // Connection cancelled
        }
        catch (Exception ex)
        {
            await HandleWebSocketErrorAsync(
                socket,
                new AppException(
                    "WS_INTERNAL",
                    "Something went wrong. Please try again.",
                    ex.Message));
        }
    }
    private async Task CloseSocketAsync(WebSocket socket)
    {
        if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            // _recentMessages.Clear();
        }

        socket.Dispose();
    }

    #endregion
    public async Task HandleIncomingMessageAsync(
    WebSocket socket,
    string messageJson)
    {
        if (string.IsNullOrWhiteSpace(messageJson))
            return;
        try
        {
            MessageEnvelope envelope;

            try
            {
                envelope = JsonSerializer.Deserialize<MessageEnvelope>(
                    messageJson,
                    JsonOptions)!;
            }
            catch (Exception ex)
            {
                throw new AppException(
                    "WS_DESERIALIZE",
                    "Failed to analyze message, please try again.",
                    $"Failed to deserialize message: {ex.Message}");
            }

            switch (envelope.Type)
            {
                case MessageType.Chat:
                    await HandleChatMessage(socket, messageJson);
                    break;

                case MessageType.Planner:
                    await HandlePlannerMessage(socket, messageJson);
                    break;

                default:
                    throw new AppException(
                        "WS_INVALID_TYPE",
                        "Invalid message type, please try again.",
                        "Unknown websocket message type.");
            }
        }
        catch (AppException ex)
        {
            await HandleWebSocketErrorAsync(socket, ex);
        }

    }
    private async Task ExecuteIntentExtraction(
    WebSocket socket,
    TravelSession session,
    TravelResponse response,
    string prompt)
    {
        var thinkingId = Guid.NewGuid().ToString();

        var extractionTask = _intentExtractionService.ExtractAsync(
                session,
                response,
                prompt);

        var animationTask = RunThinkingAnimationAsync(
                socket,
                thinkingId,
                extractionTask);

        var extractionResult = await extractionTask;

        await animationTask;

        await _utils.BroadcastAsync(
            socket,
            new ChatMessage
            {
                Type = WebSocketMessType.Chat,
                Id = thinkingId,
                Text = extractionResult.Message,
                Sender = "Bot",
                ChatType = ChatMessageType.Incoming,
                Thinking = false,
                Timestamp = DateTime.UtcNow.ToString("o")
            });

        await _utils.BroadcastStateAsync(
            socket,
            false,
            "");
    }

    private async Task BroadcastUserMessage(
    WebSocket socket,
    ChatMessageInput input)
    {
        var message = new ChatMessage
        {
            Type = WebSocketMessType.Chat,
            Id = string.IsNullOrWhiteSpace(input.Id)
                ? Guid.NewGuid().ToString()
                : input.Id,

            Text = input.Text,
            Sender = "User",
            ChatType = ChatMessageType.Outgoing,
            Timestamp = DateTime.UtcNow.ToString("o")
        };

        await _utils.BroadcastAsync(socket, message);

        await _utils.BroadcastStateAsync(
            socket,
            true,
            "AI is analyzing your travel preferences");
    }

    private async Task RunThinkingAnimationAsync(WebSocket socket, string thinkingId, Task stopSignal)
    {
        var dots = new[] { "", ".", "..", "...", "..", "." };
        var index = 0;

        while (!stopSignal.IsCompleted)
        {
            var thinkingMessage = new ChatMessage
            {
                Id = thinkingId,
                Type = WebSocketMessType.Chat,
                Text = "thinking" + dots[index],
                ChatType = ChatMessageType.Incoming,
                Sender = "Bot",
                Thinking = true
            };

            await _utils.BroadcastAsync(socket, thinkingMessage);

            index = (index + 1) % dots.Length;
            await Task.Delay(600);
        }
    }

    private async Task HandleWebSocketErrorAsync(
    WebSocket socket,
    AppException exception)
    {
        var session = GetSession(socket);

        _logger.LogError(
            exception,
            "WebSocket error. Code={Code}",
            exception.Code);

        // Reset conversation
        session.Reset();

        await _utils.BroadcastAsync(
            socket,
            new ErrorMessage
            {
                Type = WebSocketMessType.Error,
                Code = exception.Code,
                DisplayMessage = exception.DisplayMessage
            });

        // Tell frontend that the conversation can start again
        await _utils.BroadcastStateAsync(
            socket,
            false,
            "");
    }
}