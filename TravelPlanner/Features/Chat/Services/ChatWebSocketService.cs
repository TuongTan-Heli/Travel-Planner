using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TravelPlanner.Features.Chat.Models;

namespace TravelPlanner.Features.Chat.Services;

public sealed class ChatWebSocketService
{
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ConcurrentDictionary<WebSocket, object> _sockets = new();
    // private readonly List<ChatMessage> _recentMessages = new();
    // private readonly object _historyLock = new();
    private readonly ChatService _chatService;

    private readonly WebSocketNotifier _webSocketNotifier;
    private readonly IntentExtractionService _intentExtractionService;
    private readonly TravelPlanningService _travelPlanningService;
    private readonly ConcurrentDictionary<WebSocket, TravelSession> _sessions = new();
    public ChatWebSocketService(ChatService chatService, IntentExtractionService intentExtractionService, TravelPlanningService travelPlanningService, WebSocketNotifier webSocketNotifier)
    {
        _chatService = chatService;
        _intentExtractionService = intentExtractionService;
        _travelPlanningService = travelPlanningService;
        _webSocketNotifier = webSocketNotifier;
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
            _sessions.TryRemove(socket, out _);
            _sockets.TryRemove(socket, out _);
            await CloseSocketAsync(socket);
        }
    }
    private async Task ReceiveLoopAsync(WebSocket socket)
    {
        var buffer = new byte[4096];

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
                    throw new AppException("WS_MSG_TOO_LARGE", "WebSocket message too large.");
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
    private async Task CloseSocketAsync(WebSocket socket)
    {
        if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            // _recentMessages.Clear();
        }

        socket.Dispose();
    }

    // private async Task SendHistoryAsync(WebSocket socket)
    // {
    //     List<ChatMessage> history;
    //     lock (_historyLock)
    //     {
    //         history = _recentMessages.ToList();
    //     }

    //     foreach (var message in history)
    //     {
    //         await SendMessageAsync(socket, message);
    //     }
    // }

    #endregion
    private async Task BroadcastMessageAsync(WebSocket socket, ChatMessage message)
    {
        var payload = JsonSerializer.Serialize(message, SerializerOptions);
        var bytes = Encoding.UTF8.GetBytes(payload);
        var buffer = new ArraySegment<byte>(bytes);

        if (socket.State != WebSocketState.Open)
        {
            _sockets.TryRemove(socket, out _);
            await CloseSocketAsync(socket);
            return;
        }

        try
        {
            await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch
        {
            _sockets.TryRemove(socket, out _);
            await CloseSocketAsync(socket);
        }
    }


    public async Task HandleIncomingMessageAsync(WebSocket socket, string messageJson)
    {
        #region Validation and deserialization, generate user message
        if (string.IsNullOrWhiteSpace(messageJson))
        {
            return;
        }

        ChatMessageInput? input;
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        try
        {
            input = JsonSerializer.Deserialize<ChatMessageInput>(messageJson, options);
        }
        catch (Exception ex)
        {
            throw new AppException("WS_DESERIALIZE", $"Failed to deserialize message: {ex.Message}");
        }

        if (input?.Text is null or { Length: 0 })
        {
            throw new AppException("WS_INVALID_INPUT", "Message text cannot be empty.");
        }
        #endregion

        var session = GetSession(socket);

        var prompt =
                    PromptBuilder.Build(
                        TravelStage.IntentExtraction,
                        session.Context,
                        input.Text);

        var broadcastMessage = new ChatMessage
        {
            Id = string.IsNullOrWhiteSpace(input.Id) ? Guid.NewGuid().ToString() : input.Id,
            Text = input.Text,
            Type = ChatMessageType.Outgoing,
            Sender = "User",
            Timestamp = DateTime.UtcNow.ToString("o"),
            Thinking = false
        };

        // AddToHistory(broadcastMessage);
        #region Gather necessary travel information for intent extraction
        await BroadcastMessageAsync(socket, broadcastMessage);

        try
        {
            if (session.Stage == TravelStage.IntentExtraction)
            {
                var thinkingId = Guid.NewGuid().ToString();

                var extractionTask = _intentExtractionService.ExtractAsync(session, input.Text);

                var animationTask = RunThinkingAnimationAsync(socket, thinkingId, extractionTask);

                var extractionResult = await extractionTask;

                await animationTask;

                var reply = new ChatMessage
                {
                    Id = thinkingId,
                    Text = extractionResult.Message,
                    Type = ChatMessageType.Incoming,
                    Sender = "Bot",
                    Timestamp = DateTime.UtcNow.ToString("o"),
                    Thinking = false
                };

                // AddToHistory(reply);
                await BroadcastMessageAsync(socket, reply);
        #endregion
            }


            if (session.Stage == TravelStage.LocationSelection)
            {
                var TravelPlanningTask = await _travelPlanningService.BuildPlanningDataAsync(session);
                // await TravelPlanningTask;
            }
            
        }
        catch (AppException ex)
        {
            await _webSocketNotifier.SendErrorAsync(socket, ex.Code, ex.Message);
        }
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
                Text = "thinking" + dots[index],
                Type = ChatMessageType.Incoming,
                Sender = "Bot",
                Timestamp = DateTime.UtcNow.ToString("o"),
                Thinking = true
            };

            await BroadcastMessageAsync(socket, thinkingMessage);

            index = (index + 1) % dots.Length;
            await Task.Delay(600);
        }
    }
    // private void AddToHistory(ChatMessage message)
    // {
    //     lock (_historyLock)
    //     {
    //         _recentMessages.Add(message);
    //         if (_recentMessages.Count > 100)
    //         {
    //             _recentMessages.RemoveAt(0);
    //         }
    //     }
    // }

    // private void AddOrUpdateHistory(ChatMessage message)
    // {
    //     lock (_historyLock)
    //     {
    //         var index = _recentMessages.FindIndex(m => m.Id == message.Id);
    //         if (index >= 0)
    //         {
    //             _recentMessages[index] = message;
    //         }
    //         else
    //         {
    //             _recentMessages.Add(message);
    //             if (_recentMessages.Count > 100)
    //             {
    //                 _recentMessages.RemoveAt(0);
    //             }
    //         }
    //     }
    // }

    private sealed class ChatMessageInput
    {
        public string? Id { get; set; }
        public string Text { get; set; } = null!;
    }
}