using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

public class WebSocketNotifier
{
    private static readonly JsonSerializerOptions options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    public async Task SendErrorAsync(
        WebSocket socket,
        string code,
        string message)
    {
        var payload = new
        {
            Id = "error",
            Text = $"Error: {message}, Code: {code}",
            Timestamp = DateTime.UtcNow.ToString("o"),
        };

        var json = JsonSerializer.Serialize(payload, options);
        var bytes = Encoding.UTF8.GetBytes(json);

        await socket.SendAsync(
            bytes,
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
    }
}