using System.Net;

namespace CoachDesk.Desktop.Services;

public sealed class ApiException : Exception
{
    public ApiException(HttpStatusCode statusCode, string message, string? errorCode = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    public HttpStatusCode StatusCode { get; }
    public string? ErrorCode { get; }
}
