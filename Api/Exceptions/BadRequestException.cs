using System.Net;

namespace ReportChecker.Exceptions;

public class BadRequestException : ReportCheckerBaseException
{
    public BadRequestException() : base("Bad request", HttpStatusCode.BadRequest)
    {
    }

    public BadRequestException(Exception innerException) : base("Bad request", HttpStatusCode.BadRequest,
        innerException)
    {
    }

    public BadRequestException(string message) : base(message, HttpStatusCode.BadRequest)
    {
    }

    public BadRequestException(string message, Exception innerException) : base(message, HttpStatusCode.BadRequest,
        innerException)
    {
    }
}