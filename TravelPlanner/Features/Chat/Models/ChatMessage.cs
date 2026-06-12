using System.Text.Json.Serialization;

namespace TravelPlanner.Features.Chat.Models;

public enum ChatMessageType
{
    Incoming,
    Outgoing
}

public sealed class ChatMessage
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;

    [JsonIgnore]
    public ChatMessageType Type { get; set; } = ChatMessageType.Incoming;

    [JsonPropertyName("type")]
    public string TypeString
    {
        get => Type == ChatMessageType.Outgoing ? "outgoing" : "incoming";
        set => Type = string.Equals(value, "outgoing", StringComparison.OrdinalIgnoreCase)
            ? ChatMessageType.Outgoing
            : ChatMessageType.Incoming;
    }

    public string Sender { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public bool Thinking { get; set; } = false;
}