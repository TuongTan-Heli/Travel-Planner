using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace TravelPlanner.Features.Chat.Services;

public class ChatService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _apiUrl;

    public ChatService(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _apiKey = Environment.GetEnvironmentVariable("GEN_API_KEY") ?? configuration["Generative:ApiKey"] ?? string.Empty;
        _apiUrl = Environment.GetEnvironmentVariable("GEN_API_URL") ?? configuration["Generative:ApiUrl"] ?? string.Empty;
    }

    public async Task<string> GenerateReplyAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt)) return string.Empty;

        var url = _apiUrl;
        if (!string.IsNullOrEmpty(_apiKey))
        {
            url = url.Contains("?") ? $"{url}&key={_apiKey}" : $"{url}?key={_apiKey}";
        }

        // Minimal request payload — adjust if your model expects a different schema
        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.2,
                candidateCount = 1
            }
        };

        var body = JsonSerializer.Serialize(payload);
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        try
        {
            using var resp = await _http.SendAsync(req);
            var respText = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                throw new AppException("API_ERROR", $"API returned {resp.StatusCode}: {respText}");
            }

            try
            {
                using var doc = JsonDocument.Parse(respText);
                var root = doc.RootElement;

                // Try common response shapes
                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var first = candidates[0];
                    if (first.TryGetProperty("content", out var content) && content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                    {
                        var piece = parts[0];
                        if (piece.TryGetProperty("text", out var textEl)) return textEl.GetString() ?? respText;
                    }
                    if (first.TryGetProperty("output", out var outEl) && outEl.ValueKind == JsonValueKind.String) return outEl.GetString() ?? respText;
                }

                if (root.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.String)
                {
                    return output.GetString() ?? respText;
                }

                // Fallback: search for the first string value in the document
                var sb = new StringBuilder();
                foreach (var el in root.EnumerateObject())
                {
                    if (el.Value.ValueKind == JsonValueKind.String) sb.AppendLine(el.Value.GetString());
                }

                var found = sb.ToString();
                return string.IsNullOrWhiteSpace(found) ? respText : found.Trim();
            }
            catch
            {
                throw new AppException("API_RESP", respText);
            }
        }
        catch (AppException ex)
        {
            throw new AppException("API_ERROR", ex.Message, ex);
        }
    }
}

