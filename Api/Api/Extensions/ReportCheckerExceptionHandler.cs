using Microsoft.AspNetCore.Mvc;
using ReportChecker.Exceptions;

namespace ReportChecker.Api.Extensions;

public static class ReportCheckerExceptionHandler
{
    public static ActionResult ToResult(this ReportCheckerBaseException exception)
    {
        return new ObjectResult(new ProblemDetails
        {
            Status = (int)exception.StatusCode,
            Title = exception.Message,
            Detail = exception.ToString(),
        })
        {
            StatusCode = (int)exception.StatusCode
        };
    }
}