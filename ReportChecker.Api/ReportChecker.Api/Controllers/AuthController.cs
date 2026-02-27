using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Extensions;
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
        try
        {
            var credentials = await authService.GetTokenAsync(code);
            return Ok(credentials);
        }
        catch (HttpRequestException e)
        {
            return StatusCode((int)(e.StatusCode ?? HttpStatusCode.InternalServerError));
        }
    }

    [HttpPost("link")]
    [Authorize]
    public async Task<ActionResult<UserCredentials>> LinkAccount([FromQuery] string code)
    {
        try
        {
            await authService.LinkAccountAsync(code, Request.Headers.Authorization.ToString());
            return Ok();
        }
        catch (HttpRequestException e)
        {
            return StatusCode((int)(e.StatusCode ?? HttpStatusCode.InternalServerError));
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<UserCredentials>> RefreshToken([FromBody] RefreshTokenRequestSchema schema)
    {
        try
        {
            var credentials = await authService.RefreshTokenAsync(schema.RefreshToken);
            return Ok(credentials);
        }
        catch (HttpRequestException e)
        {
            return StatusCode((int)(e.StatusCode ?? HttpStatusCode.InternalServerError));
        }
    }

    [HttpGet("userinfo")]
    [Authorize]
    public async Task<ActionResult<UserInfo>> GetUserInfo(CancellationToken ct = default)
    {
        try
        {
            return Ok(await authService.GetUserInfoAsync(User.UserId));
        }
        catch (HttpRequestException e)
        {
            return StatusCode((int)(e.StatusCode ?? HttpStatusCode.InternalServerError));
        }
    }
}