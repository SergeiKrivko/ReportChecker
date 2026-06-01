using System.Net;

namespace ReportChecker.Exceptions;

public class PermissionDeniedException : ReportCheckerBaseException
{
    public PermissionDeniedException() : base("Object not found", HttpStatusCode.NotFound)
    {
    }

    public PermissionDeniedException(string message) : base(message, HttpStatusCode.NotFound)
    {
    }

    public PermissionDeniedException(string message, Exception innerException) : base(message, HttpStatusCode.NotFound,
        innerException)
    {
    }
}