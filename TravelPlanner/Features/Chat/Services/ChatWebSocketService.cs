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
    private readonly ConcurrentDictionary<WebSocket, TravelSession> _sessions = new();
    public ChatWebSocketService(ChatService chatService)
    {
        _chatService = chatService;
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

        await BroadcastMessageAsync(socket, broadcastMessage);

        try
        {
            var replyTask = _chatService.GenerateReplyAsync(prompt);
            var thinkingId = Guid.NewGuid().ToString();

            #region Thinking animation
            var animationTask = Task.Run(async () =>
                    {
                        var dots = new[] { "", ".", "..", "...", "..", "." };

                        var index = 0;
                        while (!replyTask.IsCompleted)
                        {
                            var thinking = new ChatMessage
                            {
                                Id = thinkingId,
                                Text = "thinking" + dots[index],
                                Type = ChatMessageType.Incoming,
                                Sender = "Bot",
                                Timestamp = DateTime.UtcNow.ToString("o"),
                                Thinking = true
                            };
                            // AddOrUpdateHistory(thinking);
                            await BroadcastMessageAsync(socket, thinking);

                            index = (index + 1) % dots.Length;

                            await Task.Delay(600);
                        }
                    });

            #endregion


            var replyText = await replyTask;

            await animationTask;

            var result = JsonSerializer.Deserialize<TravelIntentResult>(replyText, options);

            if (result is null)
            {
                throw new AppException("WS_INVALID_RESPONSE", "Failed to parse intent extraction result.");
            }

            MergeContext(session.Context, result);
            string rep;
            if (!session.Context.IsReadyForPlanning())
            {
                rep = result.AssistantMessage;
            }
            else
            {
                rep = "Great! I have enough information to start planning your trip.";
                session.Stage = TravelStage.LocationSelection;
            }
            #region boardcast reply to user
            var reply = new ChatMessage
            {
                Id = thinkingId,
                Text = rep,
                Type = ChatMessageType.Incoming,
                Sender = "Bot",
                Timestamp = DateTime.UtcNow.ToString("o"),
                Thinking = false
            };

            // AddToHistory(reply);
            await BroadcastMessageAsync(socket, reply);
            #endregion
        }
        catch (Exception ex)
        {
            throw new AppException("WS_ERROR", $"Error generating reply: {ex.Message}");
        }
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
