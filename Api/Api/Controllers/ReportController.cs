using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Converters;
using ReportChecker.Api.Dtos;
using ReportChecker.Api.Extensions;
using ReportChecker.Models;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
public class ReportController(
    IReportRepository reportRepository,
    IReportService reportService,
    IReportAccessService reportAccessService,
    ISubscriptionService subscriptionService,
    IYandexDiskTokenService yandexDiskTokenService,
    IGoogleDriveTokenService googleDriveTokenService) : ControllerBase
{
    [HttpGet]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<IEnumerable<ReportDto>>> GetAllReports()
    {
        var userId = User.UserId;
        var reports = await reportRepository.GetAllReportsOfUserAsync(userId);
        return Ok(reports.Select(e => e.Report.ToDto(e.Level)));
    }

    [HttpGet("{reportId:guid}")]
    [Authorize]
    public async Task<ActionResult<ReportDto>> GetReportById(Guid reportId)
    {
        var userId = User.UserId;
        var access = await reportAccessService.RequireAccessAsync(reportId, userId, ReportAccessLevel.Read);
        return Ok(access.Report.ToDto(access.Level));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Guid>> CreateReport([FromBody] CreateReportDto dto, CancellationToken ct)
    {
        var userId = User.UserId;

        // Отчёт с Диска нельзя создать без привязанного облачного аккаунта и выданного
        // токена: источник не сможет прочитать файл, а отчёт остался бы пустым.
        if (string.Equals(dto.SourceProvider, "YandexDisk", StringComparison.OrdinalIgnoreCase))
            await yandexDiskTokenService.GetAccessTokenAsync(userId, ct);
        else if (string.Equals(dto.SourceProvider, "GoogleDrive", StringComparison.OrdinalIgnoreCase))
            await googleDriveTokenService.GetAccessTokenAsync(userId, ct);

        if (!await subscriptionService.CheckReportsLimitAsync(userId, ct))
            return StatusCode(StatusCodes.Status402PaymentRequired, "Reports limit is reached");

        var reportId = await
            reportService.CreateReportAsync(userId, dto.Name, dto.Format, dto.SourceProvider,
                dto.Source.ToDomain(userId), dto.LlmModelId, dto.ImageProcessingMode, dto.ReasoningLevel);
        return Ok(reportId);
    }

    [HttpDelete("{reportId:guid}")]
    [Authorize]
    public async Task<ActionResult> DeleteReportById(Guid reportId)
    {
        await reportAccessService.RequireOwnerAsync(reportId, User.UserId);
        await reportRepository.DeleteReportAsync(reportId);
        return Ok();
    }

    [HttpPut("{reportId:guid}")]
    [Authorize]
    public async Task<ActionResult> UpdateReportById(Guid reportId, [FromBody] UpdateReportDto dto)
    {
        await reportAccessService.RequireAccessAsync(reportId, User.UserId, ReportAccessLevel.Edit);
        await reportRepository.UpdateReportAsync(reportId, dto.Name, dto.LlmModelId, dto.ImageProcessingMode,
            dto.ReasoningLevel);
        return Ok();
    }

    [HttpPost("test-source")]
    [Authorize]
    public async Task<ActionResult<SourceInfoDto>> CheckReportSource([FromBody] TestSourceRequestDto dto)
    {
        var res = await reportService.GetSourceInfoAsync(dto.Provider, dto.Source.ToDomain(User.UserId));
        return Ok(res.ToDto());
    }
}