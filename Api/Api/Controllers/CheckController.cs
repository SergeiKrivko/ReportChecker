using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Converters;
using ReportChecker.Api.Dtos;
using ReportChecker.Api.Extensions;
using ReportChecker.Exceptions;
using ReportChecker.Models;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/reports/{reportId:guid}/checks")]
public class CheckController(
    IReportAccessService reportAccessService,
    ICheckRepository checkRepository,
    IReportService reportService,
    ITaskCancellationService taskCancellationService,
    ICheckService checkService) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<CheckDto>>> GetAllChecksAsync(Guid reportId)
    {
        await reportAccessService.RequireAccessAsync(reportId, User.UserId, ReportAccessLevel.Read);
        var checks = await checkRepository.GetAllChecksOfReportAsync(reportId);
        return Ok(checks.Select(e => e.ToDto()));
    }

    [HttpGet("latest")]
    [Authorize]
    public async Task<ActionResult<CheckDto>> GetLatestCheckAsync(Guid reportId, CancellationToken ct)
    {
        await reportAccessService.RequireAccessAsync(reportId, User.UserId, ReportAccessLevel.Read);

        var check = await checkRepository.GetLatestCheckOfReportAsync(reportId, true, ct) ??
                    throw new NotFoundException("Не найдено ни одной проверки для данного отчета");

        return Ok(check.ToDto());
    }

    [HttpPost("latest/restart")]
    [Authorize]
    public async Task<ActionResult> RestartLatestCheck(Guid reportId, CancellationToken ct)
    {
        await reportAccessService.RequireAccessAsync(reportId, User.UserId, ReportAccessLevel.Edit);

        await checkService.RestartLatestCheckAsync(reportId, ct);
        return Ok();
    }

    [HttpGet("latest/chapters")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<ChapterDto>>> GetLatestCheckChaptersAsync(Guid reportId)
    {
        var access = await reportAccessService.RequireAccessAsync(reportId, User.UserId,
            ReportAccessLevel.Read);
        var check = await checkRepository.GetLatestCheckOfReportAsync(reportId) ??
                    throw new NotFoundException("Не найдено ни одной проверки для данного отчета");
        var chapters = await checkService.GetChaptersAsync(access.Report, check);
        return Ok(chapters.Select(e => e.ToDto()));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Guid>> CreateCheckAsync(Guid reportId, [FromBody] CreateCheckDto? dto)
    {
        var userId = User.UserId;

        var access = await reportAccessService.RequireAccessAsync(reportId, userId,
            ReportAccessLevel.Edit);

        var checkId = dto?.Source == null
            ? await reportService.CreateCheckAsync(access.Report)
            : await checkService.CreateCheckAsync(reportId, userId, dto.Source.ToDomain(), dto.Name);

        return Ok(checkId);
    }

    [HttpDelete("{checkId:guid}")]
    [Authorize]
    public async Task<ActionResult> CancelCheckAsync(Guid reportId, Guid checkId, CancellationToken ct)
    {
        var access = await reportAccessService.RequireAccessAsync(reportId, User.UserId,
            ReportAccessLevel.Edit);

        var check = await checkRepository.GetCheckByIdAsync(checkId, ct);
        if (check == null || check.ReportId != access.Report.Id)
            throw new NotFoundException("Проверка не найдена");

        var res = await taskCancellationService.CancelCheckAsync(checkId);
        if (!res)
            throw new NotFoundException("Проверка не найдена");
        await checkRepository.SetCheckStatusAsync(checkId, ProgressStatus.Cancelled, ct);
        return Ok();
    }
}