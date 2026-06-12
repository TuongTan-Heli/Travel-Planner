public class AppException : Exception
{
    public string Code { get; }
    public AppException(string code, string message, Exception? innerException = null) : base(message, innerException)
    {
        Code = code;
    }
}