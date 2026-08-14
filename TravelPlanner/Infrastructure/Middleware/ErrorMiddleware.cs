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
        HttpContext context,
        TravelSession session)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            await HandleExceptionAsync(context, session, ex);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        TravelSession session,
        AppException exception)
    {
        var correlationId = Guid.NewGuid().ToString();

        _logger.LogError(
            exception,
            "Application Error. Code={Code}. Message={Message}. Path={Path}",
            exception.Code,
            exception.Message,
            context.Request.Path);

        session.Stage = TravelStage.IntentExtraction;
        session.Context = new TravelPromptContext();

        if (context.Response.HasStarted)
        {
            _logger.LogWarning(
                "Response already started. CorrelationId={CorrelationId}",
                correlationId);

            return;
        }

        context.Response.StatusCode =
            StatusCodes.Status500InternalServerError;

        context.Response.ContentType = "application/json";

        var payload = new
        {
            type = "error",
            code = exception.Code,
            message = exception.DisplayMessage,
            correlationId
        };

        await context.Response.WriteAsJsonAsync(payload);
    }
}