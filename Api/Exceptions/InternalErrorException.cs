using System.Net;

namespace ReportChecker.Exceptions;

public class InternalErrorException : ReportCheckerBaseException
{
    public InternalErrorException() : base("Unexpected server error", HttpStatusCode.InternalServerError)
    {
    }

    public InternalErrorException(Exception innerException) : base("Unexpected server error",
        HttpStatusCode.InternalServerError,
        innerException)
    {
    }

    public InternalErrorException(string message) : base(message, HttpStatusCode.InternalServerError)
    {
    }

    public InternalErrorException(string message, Exception innerException) : base(message,
        HttpStatusCode.InternalServerError,
        innerException)
    {
    }
}