using System.Net;

namespace ReportChecker.Exceptions;

public abstract class ReportCheckerBaseException : Exception
{
    public HttpStatusCode StatusCode { get; } = HttpStatusCode.InternalServerError;

    public ReportCheckerBaseException(string message) : base(message)
    {
    }

    public ReportCheckerBaseException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public ReportCheckerBaseException(string message, HttpStatusCode statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

    public ReportCheckerBaseException(string message, HttpStatusCode statusCode, Exception innerException) :
        base(message, innerException)
    {
        StatusCode = statusCode;
    }
}