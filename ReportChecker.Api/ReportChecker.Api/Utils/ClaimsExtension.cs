using System.Security.Claims;

namespace ReportChecker.Api.Utils;

public static class ClaimsExtension
{
    extension(ClaimsPrincipal user)
    {
        public Guid? Id
        {
            get
            {
                var claim = user.Claims.FirstOrDefault(e => e.Type == "UserId");
                if (claim == null)
                    return null;
                return Guid.Parse(claim.Value);
            }
        }
    }
}