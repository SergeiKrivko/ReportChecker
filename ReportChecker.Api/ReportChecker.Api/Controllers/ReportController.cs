using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Extensions;
using ReportChecker.Api.Schemas;
using ReportChecker.Models;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
public class ReportController(
    IReportRepository reportRepository,
    IReportService reportService) : ControllerBase
{
    [HttpGet]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<IEnumerable<Report>>> GetAllReports()
    {
        var userId = User.UserId;
        var reports = await reportRepository.GetAllReportsOfUserAsync(userId);
        return Ok(reports);
    }

    [HttpGet("{reportId:guid}")]
    [Authorize]
    public async Task<ActionResult<Report>> GetReportById(Guid reportId)
    {
        var userId = User.UserId;
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null)
            return NotFound();
        if (report.OwnerId != userId)
            return Unauthorized();
        return Ok(report);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Guid>> CreateReport([FromBody] CreateReportSchema schema)
    {
        var userId = User.UserId;
        var reportId = await
            reportService.CreateReportAsync(userId, schema.Name, schema.Format, schema.SourceProvider,
                schema.Source);
        return Ok(reportId);
    }

    [HttpDelete("{reportId:guid}")]
    [Authorize]
    public async Task<ActionResult> DeleteReportById(Guid reportId)
    {
        var userId = User.UserId;
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null)
            return NotFound();
        if (report.OwnerId != userId)
            return Unauthorized();
        await reportRepository.DeleteReportAsync(reportId);
        return Ok();
    }

    [HttpPut("{reportId:guid}")]
    [Authorize]
    public async Task<ActionResult> UpdateReportById(Guid reportId, [FromBody] UpdateReportSchema schema)
    {
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null)
            return NotFound();
        await reportRepository.RenameReportAsync(reportId, schema.Name);
        return Ok();
    }
}