using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Schemas;
using ReportChecker.Api.Utils;
using ReportChecker.Models;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpGet("providers/{provider}/init")]
    public ActionResult<Dictionary<string, object>> InitializeAuthorization(string provider,
        string redirectUrl)
    {
        var url = authService.GetAuthorizationUrl(provider, redirectUrl);
        return Redirect(url.ToString());
    }

    [HttpPost("providers/{provider}/first")]
    public async Task<ActionResult<TokenPair>> AuthorizeFirstAccount(string provider,
        [FromBody] AccessTokenRequestSchema schema)
    {
        var userId = await authService.AuthorizeAsync(provider, schema.Parameters, schema.RedirectUrl);
        var tokenPair = await authService.CreateTokenPairAsync(userId);
        return Ok(tokenPair);
    }

    [HttpPost("providers/{provider}/second")]
    [Authorize]
    public async Task<ActionResult> AuthorizeSecondAccount(string provider,
        [FromBody] AccessTokenRequestSchema schema)
    {
        var userId = User.Id;
        if (userId == null)
            return Unauthorized();
        await authService.AuthorizeAsync(userId.Value, provider, schema.Parameters, schema.RedirectUrl);
        return Ok();
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<TokenPair>> RefreshToken([FromBody] RefreshTokenSchema schema)
    {
        var tokenPair = await authService.RefreshTokenAsync(schema.RefreshToken);
        return Ok(tokenPair);
    }

    [HttpPost("revoke")]
    public async Task<ActionResult> RevokeToken([FromBody] RefreshTokenSchema schema)
    {
        await authService.RevokeTokenAsync(schema.RefreshToken);
        return Ok();
    }
}