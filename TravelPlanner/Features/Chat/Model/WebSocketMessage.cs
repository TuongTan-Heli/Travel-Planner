public abstract class WebSocketMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Kind { get; set; } = "";

    public WebSocketMessType Type { get; set; }

    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
}

public enum WebSocketMessType
{
    Chat,
    State,
    Error,
    Close
}

public class SystemStateMessage : WebSocketMessage
{
    public SystemStateMessage()
    {
        Kind = "State";
    }
    public string Message { get; set; } = "";

    public bool Processing { get; set; }
}

public class ChatMessage : WebSocketMessage
{
    public ChatMessage()
    {
        Kind = "Chat";
    }
    public string Text { get; set; } = "";

    public ChatMessageType ChatType { get; set; }

    public string Sender { get; set; } = "";

    public bool Thinking { get; set; }
}


public enum ChatMessageType
{
    Incoming,
    Outgoing
}