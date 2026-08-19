using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;

namespace TravelPlanner.Features.Chat.Services;

public class ChatService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _api25FlashUrl;
    private readonly string _api25FlashLiteUrl;
    private readonly string _api35FlashUrl;
    private readonly string _api36FlashUrl;
    private readonly ConcurrentDictionary<string, List<string>> _userHistoryBySession = new();

    public ChatService(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _apiKey = Environment.GetEnvironmentVariable("GEN_API_KEY") ?? configuration["Generative:ApiKey"] ?? string.Empty;
        _api25FlashUrl = Environment.GetEnvironmentVariable("GEN_API_2.5_FLASH_URL") ?? configuration["Generative:ApiUrl"] ?? string.Empty;
        _api25FlashLiteUrl = Environment.GetEnvironmentVariable("GEN_API_2.5_FLASH_LITE_URL") ?? configuration["Generative:ApiUrl"] ?? string.Empty;
        _api35FlashUrl = Environment.GetEnvironmentVariable("GEN_API_3.5_FLASH_URL") ?? configuration["Generative:ApiUrl"] ?? string.Empty;
        _api36FlashUrl = Environment.GetEnvironmentVariable("GEN_API_3.6_FLASH_URL") ?? configuration["Generative:ApiUrl"] ?? string.Empty;
    }

    public async Task<string> GenerateReplyAsync(string prompt, TravelSession? session = null)
    {
        if (string.IsNullOrWhiteSpace(prompt)) return string.Empty;

        var enrichedPrompt = BuildPromptWithHistory(prompt, session);

        var url = _api25FlashUrl;
        if (!string.IsNullOrEmpty(_apiKey))
        {
            url = CreateUrl(url);
        }


        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = enrichedPrompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.2,
                candidateCount = 1,
                responseMimeType = "application/json"
            }
        };



        const int maxAttempts = 6;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
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
                    if (resp.StatusCode == HttpStatusCode.TooManyRequests || resp.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        if (attempt >= maxAttempts)
                        {
                            throw new AppException(
                                "API_RATE_LIMIT",
                                "API rate limit exceeded for models, please try again later.",
                                "Rate limit exceeded for all available API endpoints.");
                        }

                        if (url.Contains(_api25FlashUrl))
                        {
                            url = CreateUrl(_api25FlashLiteUrl);
                        }
                        else if (url.Contains(_api25FlashLiteUrl))
                        {
                            url = CreateUrl(_api35FlashUrl);
                        }
                        else if (url.Contains(_api35FlashUrl))
                        {
                            url = CreateUrl(_api36FlashUrl);
                        }
                        else
                        {
                            throw new AppException(
                                "API_RATE_LIMIT",
                                "API rate limit exceeded for models, please try again later.",
                                "Rate limit exceeded for all available API endpoints.");
                        }

                        await Task.Delay(TimeSpan.FromSeconds(6));
                        continue;
                    }

                    throw new AppException(
                        "API_ERROR",
                        "Something went wrong from our end, please try again later.",
                        $"API returned {resp.StatusCode}: {respText}");
                }

                try
                {
                    using var doc = JsonDocument.Parse(respText);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("candidates", out var candidates) &&
                        candidates.GetArrayLength() > 0)
                    {
                        var first = candidates[0];

                        if (first.TryGetProperty("content", out var content) &&
                            content.TryGetProperty("parts", out var parts) &&
                            parts.GetArrayLength() > 0 &&
                            parts[0].TryGetProperty("text", out var text))
                        {
                            return text.GetString() ?? respText;
                        }

                        if (first.TryGetProperty("output", out var output) &&
                            output.ValueKind == JsonValueKind.String)
                        {
                            return output.GetString() ?? respText;
                        }
                    }

                    if (root.TryGetProperty("output", out var rootOutput) &&
                        rootOutput.ValueKind == JsonValueKind.String)
                    {
                        return rootOutput.GetString() ?? respText;
                    }

                    var sb = new StringBuilder();
                    foreach (var property in root.EnumerateObject())
                    {
                        if (property.Value.ValueKind == JsonValueKind.String)
                            sb.AppendLine(property.Value.GetString());
                    }

                    var found = sb.ToString();
                    return string.IsNullOrWhiteSpace(found)
                        ? respText
                        : found.Trim();
                }
                catch
                {
                    throw new AppException("API_RESP", "Failed to analyze response from AI", respText);
                }
            }
            catch (AppException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new AppException("API_ERROR", "Something went wrong from our end, please try again later.", ex.Message, ex);
            }
        }

        throw new AppException("API_RATE_LIMIT", "API rate limit exceeded for models, please try again later.", "Rate limit exceeded for all available API endpoints.");
    }

    public void ClearSessionHistory(TravelSession? session)
    {
        if (session == null) return;

        var sessionId = session.SessionId;
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            _userHistoryBySession.TryRemove(sessionId, out _);
        }
    }

    private string BuildPromptWithHistory(string prompt, TravelSession? session)
    {
        if (session == null)
        {
            return prompt;
        }

        var sessionId = session.SessionId;
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            sessionId = Guid.NewGuid().ToString("N");
            session.SessionId = sessionId;
        }

        var history = _userHistoryBySession.GetOrAdd(sessionId, _ => new List<string>());
        if (history.Count == 0 || history[^1] != prompt)
        {
            history.Add(prompt);
        }

        var previousMessages = history
            .TakeLast(Math.Min(history.Count, 8))
            .Take(Math.Max(0, history.Count - 1))
            .ToList();

        if (previousMessages.Count == 0)
        {
            return prompt;
        }

        var builder = new StringBuilder();
        builder.AppendLine("Conversation history (user only):");
        foreach (var previousMessage in previousMessages)
        {
            builder.AppendLine($"- {previousMessage}");
        }

        builder.AppendLine();
        builder.AppendLine("Current user message:");
        builder.AppendLine(prompt);

        return builder.ToString();
    }

    private string CreateUrl(String url)
    {
        return url.Contains("?") ? $"{url}&key={_apiKey}" : $"{url}?key={_apiKey}";
    }
}

