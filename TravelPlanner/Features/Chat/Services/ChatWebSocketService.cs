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
    private readonly List<ChatMessage> _recentMessages = new();
    private readonly object _historyLock = new();
    private readonly ChatService _chatService;

    public ChatWebSocketService(ChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task HandleAsync(WebSocket socket)
    {
        _sockets.TryAdd(socket, new object());

        try
        {
            await SendHistoryAsync(socket);
            await ReceiveLoopAsync(socket);
        }
        finally
        {
            _sockets.TryRemove(socket, out _);
            await CloseSocketAsync(socket);
        }
    }

    private async Task SendHistoryAsync(WebSocket socket)
    {
        List<ChatMessage> history;
        lock (_historyLock)
        {
            history = _recentMessages.ToList();
        }

        foreach (var message in history)
        {
            await SendMessageAsync(socket, message);
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
                    throw new InvalidOperationException("WebSocket message too large.");
                }

                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer, count, buffer.Length - count), CancellationToken.None);
                count += result.Count;
            }

            if (count == 0)
            {
                continue;
            }

            var messageJson = Encoding.UTF8.GetString(buffer, 0, count);
            await HandleIncomingMessageAsync(messageJson);
        }
    }

    public async Task HandleIncomingMessageAsync(string messageJson)
    {
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
            Console.WriteLine($"Failed to deserialize message: {ex.Message}");
            return;
        }

        if (input?.Text is null or { Length: 0 })
        {
            Console.WriteLine("Received empty message, ignoring.");
            return;
        }

        var broadcastMessage = new ChatMessage
        {
            Id = string.IsNullOrWhiteSpace(input.Id) ? Guid.NewGuid().ToString() : input.Id,
            Text = input.Text,
            Type = ChatMessageType.Outgoing,
            Sender = "User",
            Timestamp = DateTime.UtcNow.ToString("o"),
            Thinking = false
        };

        AddToHistory(broadcastMessage);
        await BroadcastMessageAsync(broadcastMessage);
        try
        {
            var replyTask = _chatService.GenerateReplyAsync(input.Text);
            var thinkingId = Guid.NewGuid().ToString();

            // Fire-and-forget: update a single thinking message until the result is ready
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
                            AddOrUpdateHistory(thinking);
                            await BroadcastMessageAsync(thinking);

                            index = (index + 1) % dots.Length;

                            await Task.Delay(600);
                        }
                    });
            var replyText = await replyTask;
            await animationTask;

            var reply = new ChatMessage
            {
                Id = thinkingId,
                Text = replyText,
                Type = ChatMessageType.Incoming,
                Sender = "Bot",
                Timestamp = DateTime.UtcNow.ToString("o"),
                Thinking = false
            };

            AddToHistory(reply);
            await BroadcastMessageAsync(reply);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating reply: {ex.Message}");
        }
    }

    private void AddToHistory(ChatMessage message)
    {
        lock (_historyLock)
        {
            _recentMessages.Add(message);
            if (_recentMessages.Count > 100)
            {
                _recentMessages.RemoveAt(0);
            }
        }
    }

    private void AddOrUpdateHistory(ChatMessage message)
    {
        lock (_historyLock)
        {
            var index = _recentMessages.FindIndex(m => m.Id == message.Id);
            if (index >= 0)
            {
                _recentMessages[index] = message;
            }
            else
            {
                _recentMessages.Add(message);
                if (_recentMessages.Count > 100)
                {
                    _recentMessages.RemoveAt(0);
                }
            }
        }
    }

    private async Task BroadcastMessageAsync(ChatMessage message)
    {
        var payload = JsonSerializer.Serialize(message, SerializerOptions);
        var bytes = Encoding.UTF8.GetBytes(payload);
        var buffer = new ArraySegment<byte>(bytes);

        var socketList = _sockets.Keys.ToArray();
        foreach (var socket in socketList)
        {
            if (socket.State != WebSocketState.Open)
            {
                _sockets.TryRemove(socket, out _);
                continue;
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
    }

    private static Task SendMessageAsync(WebSocket socket, ChatMessage message)
    {
        var payload = JsonSerializer.Serialize(message, SerializerOptions);
        var bytes = Encoding.UTF8.GetBytes(payload);
        var buffer = new ArraySegment<byte>(bytes);
        return socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task CloseSocketAsync(WebSocket socket)
    {
        if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            _recentMessages.Clear();
        }

        socket.Dispose();
    }

    private sealed class ChatMessageInput
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}
