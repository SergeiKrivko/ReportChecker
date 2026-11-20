using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ReportChecker.Abstractions;
using ReportChecker.Api.Dtos;
using ReportChecker.Api.Extensions;
using ReportChecker.Exceptions;
using ReportChecker.Models;
using StackExchange.Redis;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/yandex")]
public class YandexDiskController(
    IYandexDiskTokenService tokenService,
    IYandexDiskClient client,
    IYandexDiskSyncService syncService,
    IReportAccessService reportAccessService,
    IConnectionMultiplexer redisConnection,
    IConfiguration configuration,
    ILogger<YandexDiskController> logger) : ControllerBase
{
    private const string StatePrefix = "yandex-oauth-state-";
    private static readonly TimeSpan StateTtl = TimeSpan.FromMinutes(10);

    private readonly IDatabase _redis = redisConnection.GetDatabase(3);

    [HttpGet("connection")]
    [Authorize]
    public async Task<ActionResult<YandexDiskConnectionDto>> GetConnection(CancellationToken ct)
    {
        var connection = await tokenService.GetConnectionAsync(User.UserId, ct);
        return Ok(new YandexDiskConnectionDto
        {
            Connected = connection.Connected,
            Login = connection.Login,
        });
    }

    [HttpGet("oauth/url")]
    [Authorize]
    public async Task<ActionResult<YandexDiskOAuthUrlDto>> GetOAuthUrl()
    {
        var state = Guid.NewGuid().ToString("N");
        await _redis.StringSetAsync(StatePrefix + state, User.UserId.ToString(), StateTtl);
        return Ok(new YandexDiskOAuthUrlDto { Url = tokenService.CreateAuthorizationUrl(state) });
    }

    [HttpGet("oauth/callback")]
    [AllowAnonymous]
    public async Task<ActionResult> OAuthCallback([FromQuery] string? code, [FromQuery] string? state,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return RedirectToFrontend("error");

        var userIdValue = await _redis.StringGetDeleteAsync(StatePrefix + state);
        if (!Guid.TryParse(userIdValue.ToString(), out var userId))
        {
            logger.LogWarning("Yandex.Disk OAuth callback with unknown state");
            return RedirectToFrontend("error");
        }

        try
        {
            await tokenService.ConnectAsync(userId, code, ct);
            return RedirectToFrontend("connected");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to connect Yandex.Disk for user '{userId}'", userId);
            return RedirectToFrontend("error");
        }
    }

    [HttpDelete("connection")]
    [Authorize]
    public async Task<ActionResult> Disconnect(CancellationToken ct)
    {
        await tokenService.DisconnectAsync(User.UserId, ct);
        return Ok();
    }

    [HttpGet("resources")]
    [Authorize]
    public async Task<ActionResult<YandexDiskFolderDto>> GetResources([FromQuery] string? path,
        [FromQuery] int offset = 0, [FromQuery] int limit = 200, CancellationToken ct = default)
    {
        var token = await tokenService.GetAccessTokenAsync(User.UserId, ct);
        var listing = await client.GetFolderAsync(token, string.IsNullOrWhiteSpace(path) ? "disk:/" : path,
            offset, limit, ct);
        return Ok(YandexDiskFolderDto.FromDomain(listing));
    }

    [HttpPost("reports/{reportId:guid}/sync")]
    [Authorize]
    public async Task<ActionResult<YandexDiskSyncResultDto>> Sync(Guid reportId, [FromQuery] bool force = false,
        CancellationToken ct = default)
    {
        await reportAccessService.RequireAccessAsync(reportId, User.UserId, ReportAccessLevel.Edit);
        var result = await syncService.SyncAsync(reportId, force, ct);
        return Ok(new YandexDiskSyncResultDto
        {
            Created = result.Created,
            CheckId = result.CheckId,
            Reason = result.Reason,
        });
    }

    private RedirectResult RedirectToFrontend(string status)
    {
        var frontend = configuration["Frontend:Url"] ?? throw new InvalidOperationException("Frontend url not set");
        return Redirect($"{frontend.TrimEnd('/')}/reports/new?source=YandexDisk&yandex={status}");
    }
}
