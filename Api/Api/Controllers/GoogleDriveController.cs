using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Dtos;
using ReportChecker.Api.Extensions;
using ReportChecker.Models;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/google")]
public class GoogleDriveController(
    IGoogleDriveTokenService tokenService,
    IGoogleDriveClient client,
    IGoogleDriveSyncService syncService,
    IReportAccessService reportAccessService) : ControllerBase
{
    [HttpGet("connection")]
    [Authorize]
    public async Task<ActionResult<GoogleDriveConnectionDto>> GetConnection(CancellationToken ct)
    {
        var connection = await tokenService.GetConnectionAsync(User.UserId, ct);
        return Ok(new GoogleDriveConnectionDto
        {
            HasAccount = connection.HasAccount,
            Connected = connection.Connected,
            Login = connection.Login,
        });
    }

    [HttpGet("resources")]
    [Authorize]
    public async Task<ActionResult<GoogleDriveFolderDto>> GetResources([FromQuery] string? folderId,
        [FromQuery] string? pageToken, [FromQuery] int pageSize = 200, CancellationToken ct = default)
    {
        var token = await tokenService.GetAccessTokenAsync(User.UserId, ct);
        var listing = await client.GetFolderAsync(token,
            string.IsNullOrWhiteSpace(folderId) ? "root" : folderId, pageToken, pageSize, ct);
        return Ok(GoogleDriveFolderDto.FromDomain(listing));
    }

    [HttpPost("reports/{reportId:guid}/sync")]
    [Authorize]
    public async Task<ActionResult<GoogleDriveSyncResultDto>> Sync(Guid reportId, [FromQuery] bool force = false,
        CancellationToken ct = default)
    {
        await reportAccessService.RequireAccessAsync(reportId, User.UserId, ReportAccessLevel.Edit);
        var result = await syncService.SyncAsync(reportId, force, ct);
        return Ok(new GoogleDriveSyncResultDto
        {
            Created = result.Created,
            CheckId = result.CheckId,
            Reason = result.Reason,
        });
    }
}
