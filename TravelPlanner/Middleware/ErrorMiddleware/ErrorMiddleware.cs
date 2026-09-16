using System.Net.WebSockets;
using System.Text.Json;

public class ErrorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorMiddleware> _logger;
    private readonly ErrorHandlerService _errorHandlerService;

    public ErrorMiddleware(
        RequestDelegate next,
        ILogger<ErrorMiddleware> logger,
        ErrorHandlerService errorHandlerService)
    {
        _next = next;
        _logger = logger;
        _errorHandlerService = errorHandlerService;
    }

    public async Task InvokeAsync(
        HttpContext context,
        TravelSession session)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            var payload = await _errorHandlerService.HandleErrorAsync(session, ex, context.Request.Path);

            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Response already started. CorrelationId={CorrelationId}", payload.CorrelationId);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(payload);
        }
    }
}