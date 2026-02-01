using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Schemas;
using ReportChecker.Models;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/reports/{reportId:guid}/checks")]
public class CheckController(
    IAuthService authService,
    IReportRepository reportRepository,
    ICheckRepository checkRepository,
    IReportService reportService,
    ICheckService checkService) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<Check>>> GetAllChecksAsync(Guid reportId)
    {
        var userId = await authService.AuthenticateAsync(User);
        if (userId == null)
            return Unauthorized();
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null)
            return NotFound();
        if (report.OwnerId != userId)
            return Unauthorized();
        var check = await checkRepository.GetAllChecksOfReportAsync(reportId);
        return Ok(check);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Guid>> CreateCheckAsync(Guid reportId, [FromBody] CreateCheckSchema? schema)
    {
        var userId = await authService.AuthenticateAsync(User);
        if (userId == null)
            return Unauthorized();

        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null)
            return NotFound();
        if (report.OwnerId != userId)
            return Unauthorized();

        var checkId = schema?.Source == null
            ? await reportService.CreateCheckAsync(report)
            : await checkService.CreateCheckAsync(reportId, userId.Value, schema.Source, schema.Name);

        return Ok(checkId);
    }
}