public class AppException : Exception
{
    public string Code { get; }
    public string DisplayMessage { get; }
    public AppException(string code, string displayMessage, string message, Exception? innerException = null) : base(message, innerException)
    {
        Code = code;
        DisplayMessage = displayMessage;
    }
}