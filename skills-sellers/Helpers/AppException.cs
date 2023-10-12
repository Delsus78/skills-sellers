namespace skills_sellers.Helpers;

public class AppException : Exception
{
    public int ErrorCode { get; set; }

    public AppException(string message, int errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }
}