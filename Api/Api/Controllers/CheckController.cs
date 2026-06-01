using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Extensions;
using ReportChecker.Api.Schemas;
using ReportChecker.Exceptions;
using ReportChecker.Models;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/reports/{reportId:guid}/checks")]
public class CheckController(
    IReportRepository reportRepository,
    ICheckRepository checkRepository,
    IReportService reportService,
    ITaskCancellationService taskCancellationService,
    ICheckService checkService) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<Check>>> GetAllChecksAsync(Guid reportId)
    {
        var userId = User.UserId;
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null || report.OwnerId != userId)
            throw new NotFoundException($"Отчет '{reportId}' не найден либо доступ заблокирован");
        var checks = await checkRepository.GetAllChecksOfReportAsync(reportId);
        return Ok(checks);
    }

    [HttpGet("latest")]
    [Authorize]
    public async Task<ActionResult<Check>> GetLatestCheckAsync(Guid reportId, CancellationToken ct)
    {
        var userId = User.UserId;
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null || report.OwnerId != userId)
            throw new NotFoundException($"Отчет '{reportId}' не найден либо доступ заблокирован");

        var check = await checkRepository.GetLatestCheckOfReportAsync(reportId, true, ct) ??
                    throw new NotFoundException("Не найдено ни одной проверки для данного отчета");

        return Ok(check);
    }

    [HttpPost("latest/restart")]
    [Authorize]
    public async Task<ActionResult> RestartLatestCheck(Guid reportId, CancellationToken ct)
    {
        await checkService.RestartLatestCheckAsync(reportId, ct);
        return Ok();
    }

    [HttpGet("latest/chapters")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<Chapter>>> GetLatestCheckChaptersAsync(Guid reportId)
    {
        var userId = User.UserId;
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null || report.OwnerId != userId)
            throw new NotFoundException($"Отчет '{reportId}' не найден либо доступ заблокирован");
        var check = await checkRepository.GetLatestCheckOfReportAsync(reportId) ??
                    throw new NotFoundException("Не найдено ни одной проверки для данного отчета");
        var chapters = await checkService.GetChaptersAsync(report, check);
        return Ok(chapters);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Guid>> CreateCheckAsync(Guid reportId, [FromBody] CreateCheckSchema? schema)
    {
        var userId = User.UserId;

        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null || report.OwnerId != userId)
            throw new NotFoundException($"Отчет '{reportId}' не найден либо доступ заблокирован");

        var checkId = schema?.Source == null
            ? await reportService.CreateCheckAsync(report)
            : await checkService.CreateCheckAsync(reportId, userId, schema.Source, schema.Name);

        return Ok(checkId);
    }

    [HttpDelete("{checkId:guid}")]
    [Authorize]
    public async Task<ActionResult> CancelCheckAsync(Guid reportId, Guid checkId, CancellationToken ct)
    {
        var res = await taskCancellationService.CancelCheckAsync(checkId);
        if (!res)
            throw new NotFoundException("Проверка не найдена");
        await checkRepository.SetCheckStatusAsync(checkId, ProgressStatus.Cancelled, ct);
        return Ok();
    }
}