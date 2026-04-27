using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Extensions;
using ReportChecker.Models;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/statistics")]
public class StatisticsController(ILlmUsageRepository llmUsageRepository) : ControllerBase
{
    [HttpGet("usage")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<LlmUsageGroup>>> GetReportsUsage(
        [FromQuery(Name = "start")] DateTime? timeStart = null,
        [FromQuery(Name = "end")] DateTime? timeEnd = null,
        CancellationToken ct = default)
    {
        var userId = User.UserId;
        timeEnd ??= DateTime.UtcNow;
        timeStart ??= timeEnd.Value.AddDays(-30);
        var usage = await llmUsageRepository.GetUsageStatisticsAsync(userId, timeStart.Value, timeEnd.Value, ct);
        return Ok(usage);
    }

    [HttpGet("timeUsage")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<LlmUsageInterval>>> GetUsageByTime(
        [FromQuery(Name = "start")] DateTime? timeStart = null,
        [FromQuery(Name = "end")] DateTime? timeEnd = null,
        [FromQuery(Name = "report")] Guid? reportId = null,
        [FromQuery(Name = "model")] Guid? modelId = null,
        [FromQuery(Name = "intervals")] int? numberOfIntervals = null,
        CancellationToken ct = default)
    {
        var userId = User.UserId;
        timeEnd ??= DateTime.UtcNow;
        timeStart ??= timeEnd.Value.AddDays(-30);
        var usage = await llmUsageRepository.GetTimeUsageAsync(userId, timeStart.Value, timeEnd.Value, modelId,
            reportId, numberOfIntervals, ct);
        return Ok(usage);
    }
}