using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Converters;
using ReportChecker.Api.Dtos;
using ReportChecker.Api.Extensions;
using ReportChecker.Exceptions;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
public class ReportController(
    IReportRepository reportRepository,
    IReportService reportService,
    ISubscriptionService subscriptionService) : ControllerBase
{
    [HttpGet]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<IEnumerable<ReportDto>>> GetAllReports()
    {
        var userId = User.UserId;
        var reports = await reportRepository.GetAllReportsOfUserAsync(userId);
        return Ok(reports.Select(e => e.ToDto()));
    }

    [HttpGet("{reportId:guid}")]
    [Authorize]
    public async Task<ActionResult<ReportDto>> GetReportById(Guid reportId)
    {
        var userId = User.UserId;
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null || report.OwnerId != userId)
            throw new NotFoundException($"Отчет '{reportId}' не найден либо доступ заблокирован");
        return Ok(report.ToDto());
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Guid>> CreateReport([FromBody] CreateReportDto dto, CancellationToken ct)
    {
        var userId = User.UserId;

        if (!await subscriptionService.CheckReportsLimitAsync(userId, ct))
            return StatusCode(StatusCodes.Status402PaymentRequired, "Reports limit is reached");

        var reportId = await
            reportService.CreateReportAsync(userId, dto.Name, dto.Format, dto.SourceProvider,
                dto.Source.ToDomain(), dto.LlmModelId, dto.ImageProcessingMode, dto.ReasoningLevel);
        return Ok(reportId);
    }

    [HttpDelete("{reportId:guid}")]
    [Authorize]
    public async Task<ActionResult> DeleteReportById(Guid reportId)
    {
        var userId = User.UserId;
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null || report.OwnerId != userId)
            throw new NotFoundException($"Отчет '{reportId}' не найден либо доступ заблокирован");
        await reportRepository.DeleteReportAsync(reportId);
        return Ok();
    }

    [HttpPut("{reportId:guid}")]
    [Authorize]
    public async Task<ActionResult> UpdateReportById(Guid reportId, [FromBody] UpdateReportDto dto)
    {
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null || report.OwnerId != User.UserId)
            throw new NotFoundException($"Отчет '{reportId}' не найден либо доступ заблокирован");
        await reportRepository.UpdateReportAsync(reportId, dto.Name, dto.LlmModelId, dto.ImageProcessingMode,
            dto.ReasoningLevel);
        return Ok();
    }

    [HttpPost("test-source")]
    [Authorize]
    public async Task<ActionResult<SourceInfoDto>> CheckReportSource([FromBody] TestSourceRequestDto dto)
    {
        var res = await reportService.GetSourceInfoAsync(dto.Provider, dto.Source.ToDomain());
        return Ok(res.ToDto());
    }
}