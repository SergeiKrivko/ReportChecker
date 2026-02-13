using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Schemas;
using ReportChecker.Models;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpGet("{provider}")]
    public ActionResult StartAuthorization(string provider, [FromQuery] string redirectUrl)
    {
        return Redirect(authService.GetAuthUrl(provider, redirectUrl));
    }

    [HttpPost("token")]
    public async Task<ActionResult<UserCredentials>> GetToken([FromQuery] string code)
    {
        var credentials = await authService.GetTokenAsync(code);
        return Ok(credentials);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<UserCredentials>> RefreshToken([FromBody] RefreshTokenRequestSchema schema)
    {
        var credentials = await authService.RefreshTokenAsync(schema.RefreshToken);
        return Ok(credentials);
    }
}