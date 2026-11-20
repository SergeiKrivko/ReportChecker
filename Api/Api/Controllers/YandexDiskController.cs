using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Dtos;
using ReportChecker.Api.Extensions;
using ReportChecker.Models;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/yandex")]
public class YandexDiskController(
    IYandexDiskTokenService tokenService,
    IYandexDiskClient client,
    IYandexDiskSyncService syncService,
    IReportAccessService reportAccessService) : ControllerBase
{
    [HttpGet("connection")]
    [Authorize]
    public async Task<ActionResult<YandexDiskConnectionDto>> GetConnection(CancellationToken ct)
    {
        var connection = await tokenService.GetConnectionAsync(User.UserId, ct);
        return Ok(new YandexDiskConnectionDto
        {
            HasAccount = connection.HasAccount,
            Connected = connection.Connected,
            Login = connection.Login,
        });
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
}
