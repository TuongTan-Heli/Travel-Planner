// Custom middleware for request handling
using System.Text.Json;

public class ErrorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorMiddleware> _logger;

    public ErrorMiddleware(
        RequestDelegate next,
        ILogger<ErrorMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        AppException exception)
    {
        var correlationId = Guid.NewGuid().ToString();

        // Always log the error, regardless of response state
        _logger.LogError(
            exception,
            "Error. CorrelationId={CorrelationId}. Path={Path}",
            correlationId,
            context.Request.Path);

        // Only modify response if it hasn't started yet
        if (context.Response.HasStarted)
        {
            _logger.LogWarning(
                "Response has already started. Cannot send error response body for CorrelationId={CorrelationId}",
                correlationId);
            return;
        }

        context.Response.ContentType = "application/json";

        var response = new Error
        {
            Code = exception.Code,
            Message = exception.Message,
            CorrelationId = correlationId,
            Path = context.Request.Path,
            Timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}