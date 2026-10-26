using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Converters;
using ReportChecker.Api.Dtos;
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
    public ActionResult<IEnumerable<BenchmarkCaseDto>> GetCases()
    {
        return Ok(benchmarkCaseProvider.GetCases().Select(e => e.ToDto()));
    }

    /// <summary>
    /// Текст теста, разбитый на главы и строки. Строки пронумерованы так же, как их видит
    /// модель (внутри главы с единицы), поэтому номера совпадают с полем <c>line</c>
    /// известных ошибок из <c>GET /cases</c>.
    /// </summary>
    [HttpGet("cases/{caseId}/chapters")]
    [AllowAnonymous]
    public async Task<ActionResult<BenchmarkCaseTextDto>> GetCaseChaptersAsync(string caseId,
        CancellationToken ct = default)
    {
        var benchmarkCase = benchmarkCaseProvider.GetCase(caseId) ??
                            throw new NotFoundException($"Тест '{caseId}' не найден");
        if (benchmarkCase.ValidationError != null)
            throw new BadRequestException($"Тест '{caseId}' некорректен: {benchmarkCase.ValidationError}");
        var text = await benchmarkCaseProvider.GetTextAsync(benchmarkCase, ct);
        return Ok(text.ToDto());
    }

    /// <summary>Запускает прогоны (по одному на пару тест–модель) и возвращает их идентификаторы.</summary>
    [HttpPost("runs")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<IReadOnlyList<Guid>>> CreateRunsAsync(CreateBenchmarkRunDto dto,
        CancellationToken ct = default)
    {
        var runIds = await benchmarkService.CreateRunsAsync(new BenchmarkRunRequest
        {
            CaseIds = dto.CaseIds,
            ModelIds = dto.ModelIds,
            UseLlmMatching = dto.UseLlmMatching,
            ReasoningEffort = dto.ReasoningEffort,
        }, ct);
        return Ok(runIds);
    }

    /// <summary>
    /// Прогоны, отсортированные по времени создания (сначала новые). Мягко удалённые
    /// скрыты, пока явно не запрошены через <paramref name="includeDeleted"/>.
    /// </summary>
    [HttpGet("runs")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<BenchmarkRunDto>>> GetRunsAsync(string? caseId = null,
        Guid? modelId = null, ProgressStatus? status = null, LlmReasoningEffort? reasoning = null,
        int limit = 50, int offset = 0, bool includeDeleted = false, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 200);
        offset = Math.Max(0, offset);
        var runs = await benchmarkService.GetRunsAsync(caseId, modelId, status, reasoning, limit, offset,
            includeDeleted, ct);
        return Ok(runs.Select(e => e.ToDto()));
    }

    /// <summary>
    /// Один прогон со всеми строками результатов. Удалённый отдаётся только при
    /// <paramref name="includeDeleted"/> — тогда страница прогона показывает предупреждение
    /// вместо «не найден».
    /// </summary>
    [HttpGet("runs/{runId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<BenchmarkRunDto>> GetRunAsync(Guid runId, bool includeDeleted = false,
        CancellationToken ct = default)
    {
        var run = await benchmarkService.GetRunAsync(runId, includeDeleted, ct) ??
                  throw new NotFoundException($"Прогон '{runId}' не найден");
        return Ok(run.ToDto());
    }

    /// <summary>Сводка по всем неудалённым прогонам, агрегированная по тройке тест–модель–уровень рассуждений.</summary>
    [HttpGet("summary")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<BenchmarkSummaryDto>>> GetSummaryAsync(string? caseId = null,
        Guid? modelId = null, CancellationToken ct = default)
    {
        var summary = await benchmarkService.GetSummaryAsync(caseId, modelId, ct);
        return Ok(summary.Select(e => e.ToDto()));
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