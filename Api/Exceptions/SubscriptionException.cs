using System.Net;

namespace ReportChecker.Exceptions;

public class SubscriptionException : ReportCheckerBaseException
{
    public SubscriptionException() : base("Can not create subscription", HttpStatusCode.Conflict)
    {
    }

    public SubscriptionException(Exception innerException) : base("Can not create subscription", HttpStatusCode.Conflict,
        innerException)
    {
    }

    public SubscriptionException(string message) : base(message, HttpStatusCode.Conflict)
    {
    }

    public SubscriptionException(string message, Exception innerException) : base(message, HttpStatusCode.Conflict,
        innerException)
    {
    }
}