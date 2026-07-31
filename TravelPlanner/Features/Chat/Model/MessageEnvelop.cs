using System.Text.Json;

public sealed class MessageEnvelope
{
    public string Id { get; set; } = string.Empty;

    public MessageType Type { get; set; }

    public JsonElement Data { get; set; }
}

public enum MessageType
{
    Chat,
    Planner
}