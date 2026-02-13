using System.Security.Claims;

namespace ReportChecker.Api.Extensions;

public static class ClaimsPrincipalExtension
{
    extension(ClaimsPrincipal principal)
    {
        public Guid UserId =>
            Guid.Parse(principal.FindFirst("UserId")?.Value ?? throw new Exception("'UserId' claim was not found"));
    }
}