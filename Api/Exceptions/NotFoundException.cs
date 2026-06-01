using System.Net;

namespace ReportChecker.Exceptions;

public class NotFoundException : ReportCheckerBaseException
{
    public NotFoundException() : base("Object not found", HttpStatusCode.NotFound)
    {
    }

    public NotFoundException(Exception innerException) : base("Object not found", HttpStatusCode.NotFound,
        innerException)
    {
    }

    public NotFoundException(string message) : base(message, HttpStatusCode.NotFound)
    {
    }

    public NotFoundException(string message, Exception innerException) : base(message, HttpStatusCode.NotFound,
        innerException)
    {
    }
}