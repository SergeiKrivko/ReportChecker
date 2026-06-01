using System.Net;

namespace ReportChecker.Exceptions;

public class UnauthorizedException : ReportCheckerBaseException
{
    public UnauthorizedException() : base("Object not found", HttpStatusCode.Unauthorized)
    {
    }

    public UnauthorizedException(Exception innerException) : base("Object not found", HttpStatusCode.Unauthorized,
        innerException)
    {
    }

    public UnauthorizedException(string message) : base(message, HttpStatusCode.Unauthorized)
    {
    }

    public UnauthorizedException(string message, Exception innerException) : base(message, HttpStatusCode.Unauthorized,
        innerException)
    {
    }
}