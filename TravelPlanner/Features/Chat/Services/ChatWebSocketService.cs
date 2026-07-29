using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TravelPlanner.Features.Chat.Services;

public sealed class ChatWebSocketService
{
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
    private static readonly string THINKING_ID = "Thinking";
    private static readonly string STATE_ID = "STATE";
    private readonly ConcurrentDictionary<WebSocket, object> _sockets = new();
    private readonly WebSocketNotifier _webSocketNotifier;
    private readonly IntentExtractionService _intentExtractionService;
    private readonly TravelPlanningService _travelPlanningService;
    private readonly ScoringService _scoringService;
    private readonly ConcurrentDictionary<WebSocket, TravelSession> _sessions = new();
    private readonly SetupItineraryService _setupItineraryService;
    private readonly PresentationService _presentationService;
    private readonly ChatService _chatService;
    public ChatWebSocketService(IntentExtractionService intentExtractionService, TravelPlanningService travelPlanningService, WebSocketNotifier webSocketNotifier,
                                ScoringService scoringService, SetupItineraryService setupItineraryService, PresentationService presentationService, ChatService chatService)
    {
        _intentExtractionService = intentExtractionService;
        _travelPlanningService = travelPlanningService;
        _webSocketNotifier = webSocketNotifier;
        _scoringService = scoringService;
        _setupItineraryService = setupItineraryService;
        _presentationService = presentationService;
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
            _sessions.TryRemove(socket, out var session);
            _sockets.TryRemove(socket, out _);
            _chatService.ClearSessionHistory(session);
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

    #endregion
    private async Task BroadcastAsync(
    WebSocket socket,
    WebSocketMessage message)
    {
        var payload = JsonSerializer.Serialize(
            message,
            message.GetType(),
            SerializerOptions
        );

        var bytes = Encoding.UTF8.GetBytes(payload);

        await socket.SendAsync(
            bytes,
            WebSocketMessageType.Text,
            true,
            CancellationToken.None
        );
    }

    public async Task HandleIncomingMessageAsync(WebSocket socket, string messageJson)
    {
        #region Validation and deserialization, generate user message
        if (string.IsNullOrWhiteSpace(messageJson))
        {
            return;
        }

        ChatMessageInput? input;
        try
        {
            input = JsonSerializer.Deserialize<ChatMessageInput>(messageJson,
            new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            });
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
        var travelResponse = new TravelResponse();
        var broadcastMessage = new ChatMessage
        {
            Type = WebSocketMessType.Chat,
            Id = string.IsNullOrWhiteSpace(input.Id) ? Guid.NewGuid().ToString() : input.Id,
            Text = input.Text,
            ChatType = ChatMessageType.Outgoing,
            Sender = "User",
            Timestamp = DateTime.UtcNow.ToString("o"),
            Thinking = false
        };

        // AddToHistory(broadcastMessage);
        #region Gather necessary travel information for intent extraction
        await BroadcastAsync(socket, broadcastMessage);
        await SendStateAsync(socket, true, "AI is analyzing your travel preferences");
        try
        {
            if (session.Stage == TravelStage.IntentExtraction)
            {
                var thinkingId = Guid.NewGuid().ToString();

                var extractionTask = _intentExtractionService.ExtractAsync(session, travelResponse, input.Text);

                var animationTask = RunThinkingAnimationAsync(socket, thinkingId, extractionTask);

                var extractionResult = await extractionTask;

                var reply = new ChatMessage
                {
                    Type = WebSocketMessType.Chat,
                    Id = thinkingId,
                    Text = extractionResult.Message,
                    ChatType = ChatMessageType.Incoming,
                    Sender = "Bot",
                    Timestamp = DateTime.UtcNow.ToString("o"),
                    Thinking = false
                };

                await animationTask;

                // AddToHistory(reply);
                await BroadcastAsync(socket, reply);
                await SendStateAsync(socket, false, "");

                #endregion
            }


            if (session.Stage == TravelStage.LocationSelection)
            {
                await SendStateAsync(socket, true, "Selecting best location");
                travelResponse.TripPlanningData = await _travelPlanningService.BuildPlanningDataAsync(session);
            }

            if (session.Stage == TravelStage.Scoring)
            {
                await SendStateAsync(socket, true, "Scoring places");
                travelResponse.TripPlanningData.RecommendedPlaces = await _scoringService.ScorePlaces(travelResponse.TripPlanningData.RecommendedPlaces, session);
            }

            if (session.Stage == TravelStage.SetupItinerary)
            {
                await SendStateAsync(socket, true, "Setting up your trip");
                travelResponse.Itinerary = await _setupItineraryService.Setup(travelResponse, session);
            }

            if (session.Stage == TravelStage.FinalPresentation)
            {
                await SendStateAsync(socket, true, "Preparing final presentation");
                travelResponse.FinalPresentation = await _presentationService.Present(travelResponse, session);

                var reply = new ChatMessage
                {
                    Type = WebSocketMessType.Chat,
                    Id = "Presentation",
                    Text = JsonSerializer.Serialize(
                        travelResponse.FinalPresentation,
                        new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        }),
                    ChatType = ChatMessageType.Incoming,
                    Sender = "Bot",
                    Timestamp = DateTime.UtcNow.ToString("o"),
                    Thinking = false
                };

                await BroadcastAsync(socket, reply);
                await SendStateAsync(socket, false, "");

                session.Stage = TravelStage.IntentExtraction;

            }
            // AddToHistory(reply);

        }
        catch (AppException ex)
        {
            await _webSocketNotifier.SendErrorAsync(socket, ex.Code, ex.Message);
        }
    }

    private async Task SendStateAsync(
    WebSocket socket,
    bool processing,
    string message)
    {
        await BroadcastAsync(socket, new SystemStateMessage
        {
            Id = STATE_ID,
            Type = WebSocketMessType.State,
            Message = message,
            Processing = processing
        });
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

            await BroadcastAsync(socket, thinkingMessage);

            index = (index + 1) % dots.Length;
            await Task.Delay(600);
        }
    }

    private sealed class ChatMessageInput
    {
        public string? Id { get; set; }
        public string Text { get; set; } = null!;
    }
}