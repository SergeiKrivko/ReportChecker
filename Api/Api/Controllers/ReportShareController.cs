using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Converters;
using ReportChecker.Api.Dtos;
using ReportChecker.Api.Extensions;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/reports/{reportId:guid}/share")]
[Authorize]
public class ReportShareController(IReportShareService shareService) : ControllerBase
{
    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<ReportShareDto>>> GetSharesAsync(Guid reportId,
        CancellationToken ct)
    {
        var shares = await shareService.GetSharesAsync(reportId, User.UserId, ct);
        return Ok(shares.Select(e => e.ToDto()));
    }

    [HttpPost("users")]
    public async Task<ActionResult<ReportShareDto>> ShareWithUserAsync(Guid reportId,
        [FromBody] CreateReportShareDto dto, CancellationToken ct)
    {
        var share = await shareService.ShareWithUserAsync(reportId, User.UserId, dto.LoginOrEmail,
            dto.AccessLevel, ct);
        return Ok(share.ToDto());
    }

    [HttpPut("users/{userId:guid}")]
    public async Task<ActionResult<ReportShareDto>> UpdateShareAsync(Guid reportId, Guid userId,
        [FromBody] UpdateReportShareDto dto, CancellationToken ct)
    {
        var share = await shareService.UpdateShareAsync(reportId, User.UserId, userId, dto.AccessLevel, ct);
        return Ok(share.ToDto());
    }

    [HttpDelete("users/{userId:guid}")]
    public async Task<ActionResult> RevokeShareAsync(Guid reportId, Guid userId, CancellationToken ct)
    {
        await shareService.RevokeShareAsync(reportId, User.UserId, userId, ct);
        return Ok();
    }

    [HttpGet("link")]
    public async Task<ActionResult<ReportShareLinkDto>> GetLinkAsync(Guid reportId, CancellationToken ct)
    {
        var link = await shareService.GetLinkAsync(reportId, User.UserId, ct);
        return link == null ? NoContent() : Ok(link.ToDto());
    }

    [HttpPost("link")]
    public async Task<ActionResult<ReportShareLinkDto>> CreateLinkAsync(Guid reportId, CancellationToken ct)
    {
        var link = await shareService.CreateLinkAsync(reportId, User.UserId, ct);
        return Ok(link.ToDto());
    }

    [HttpPost("link/rotate")]
    public async Task<ActionResult<ReportShareLinkDto>> RotateLinkAsync(Guid reportId, CancellationToken ct)
    {
        var link = await shareService.RotateLinkAsync(reportId, User.UserId, ct);
        return Ok(link.ToDto());
    }

    [HttpDelete("link")]
    public async Task<ActionResult> RevokeLinkAsync(Guid reportId, CancellationToken ct)
    {
        await shareService.RevokeLinkAsync(reportId, User.UserId, ct);
        return Ok();
    }
}
