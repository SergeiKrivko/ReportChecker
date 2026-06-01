using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Exceptions;

namespace ReportChecker.Api.BackgroundServices;

public class ExceptionsHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var statusCode = (exception as ReportCheckerBaseException)?.StatusCode ??
                         HttpStatusCode.InternalServerError;
        httpContext.Response.StatusCode = (int)statusCode;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = (int)statusCode,
            Title = exception.Message,
            Detail = exception.ToString(),
        }, ct);

        return true;
    }
}