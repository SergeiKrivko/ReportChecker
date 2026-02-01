using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Schemas;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("{provider}")]
    public async Task<ActionResult<Dictionary<string, object>>> HandleAuthorization(string provider,
        [FromBody] AccessTokenRequestSchema schema)
    {
        var authProvider = authService.GetAuthProvider(provider);
        if (authProvider is null)
            return NotFound();
        var credentials = await authProvider.AuthorizeAsync(schema.Parameters, schema.RedirectUrl);
        if (credentials is null)
            return Unauthorized();
        return Ok(credentials);
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<Guid>> GetAccountInfo()
    {
        var userId = await authService.AuthenticateOrCreateUserAsync(User);
        return Ok(userId);
    }
}