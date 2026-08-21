// Error handling and logging utilities
public static class ErrorHandler
{
    public static void LogError(ILogger logger,Exception ex, string context = "")
    {
       logger.LogError(ex, $"Error in {context}: {ex.Message}");
    }

    public static void LogWarning(ILogger logger, string message, string context = "")
    {
        logger.LogWarning($"Warning in {context}: {message}");
    }

    public static void LogInfo(ILogger logger, string message)
    {
        logger.LogInformation(message);
    }
}