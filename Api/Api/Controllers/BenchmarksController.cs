using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Schemas;
using ReportChecker.Exceptions;
using ReportChecker.Models;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/benchmarks")]
public class BenchmarksController(
    IBenchmarkService benchmarkService,
    IBenchmarkCaseProvider benchmarkCaseProvider) : ControllerBase
{
    /// <summary>Список тестов бенчмарка с описанием известных ошибок.</summary>
    [HttpGet("cases")]
    [AllowAnonymous]
    public ActionResult<IEnumerable<BenchmarkCase>> GetCases()
    {
        return Ok(benchmarkCaseProvider.GetCases());
    }

    /// <summary>Запускает прогоны (по одному на пару тест–модель) и возвращает их идентификаторы.</summary>
    [HttpPost("runs")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<IReadOnlyList<Guid>>> CreateRunsAsync(CreateBenchmarkRunSchema schema,
        CancellationToken ct = default)
    {
        var runIds = await benchmarkService.CreateRunsAsync(new BenchmarkRunRequest
        {
            CaseIds = schema.CaseIds,
            ModelIds = schema.ModelIds,
            UseLlmMatching = schema.UseLlmMatching,
            ReasoningEffort = schema.ReasoningEffort,
        }, ct);
        return Ok(runIds);
    }

    /// <summary>Прогоны, отсортированные по времени создания (сначала новые), без удалённых.</summary>
    [HttpGet("runs")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<BenchmarkRun>>> GetRunsAsync(string? caseId = null,
        Guid? modelId = null, ProgressStatus? status = null, LlmReasoningEffort? reasoning = null,
        int limit = 50, int offset = 0, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 200);
        offset = Math.Max(0, offset);
        var runs = await benchmarkService.GetRunsAsync(caseId, modelId, status, reasoning, limit, offset, ct);
        return Ok(runs);
    }

    /// <summary>Один прогон со всеми строками результатов.</summary>
    [HttpGet("runs/{runId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<BenchmarkRun>> GetRunAsync(Guid runId, CancellationToken ct = default)
    {
        var run = await benchmarkService.GetRunAsync(runId, ct) ??
                  throw new NotFoundException($"Прогон '{runId}' не найден");
        return Ok(run);
    }

    /// <summary>Сводка по всем неудалённым прогонам, агрегированная по тройке тест–модель–уровень рассуждений.</summary>
    [HttpGet("summary")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<BenchmarkSummary>>> GetSummaryAsync(string? caseId = null,
        Guid? modelId = null, CancellationToken ct = default)
    {
        var summary = await benchmarkService.GetSummaryAsync(caseId, modelId, ct);
        return Ok(summary);
    }

    /// <summary>
    /// Удаляет прогон: активный сначала отменяется, затем запись помечается удалённой.
    /// </summary>
    [HttpDelete("runs/{runId:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult> DeleteRunAsync(Guid runId, CancellationToken ct = default)
    {
        var deleted = await benchmarkService.DeleteRunAsync(runId, ct);
        if (!deleted)
            throw new NotFoundException($"Прогон '{runId}' не найден");
        return Ok();
    }
}
