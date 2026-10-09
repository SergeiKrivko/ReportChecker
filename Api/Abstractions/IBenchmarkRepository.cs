using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IBenchmarkRepository
{
    /// <summary>Создаёт прогон в статусе <see cref="ProgressStatus.Queued"/>.</summary>
    public Task<Guid> CreateRunAsync(string caseId, string caseName, Guid modelId,
        LlmReasoningEffort reasoningEffort = LlmReasoningEffort.None, CancellationToken ct = default);

    /// <summary>
    /// Меняет статус прогона. При переходе в <see cref="ProgressStatus.InProgress"/> проставляет
    /// <c>StartedAt</c>, при завершении (<c>Completed</c>/<c>Failed</c>/<c>Cancelled</c>) — <c>FinishedAt</c>.
    /// </summary>
    public Task<bool> SetStatusAsync(Guid runId, ProgressStatus status, CancellationToken ct = default);

    /// <summary>Сохраняет результаты и агрегаты успешного прогона.</summary>
    public Task<bool> CompleteRunAsync(Guid runId, IReadOnlyCollection<BenchmarkResult> results,
        BenchmarkRun aggregates, BenchmarkUsage usage, DateTime finishedAt, CancellationToken ct = default);

    /// <summary>Помечает прогон как упавший, сохраняя время завершения.</summary>
    public Task<bool> FailRunAsync(Guid runId, string reason, DateTime finishedAt, CancellationToken ct = default);

    /// <summary>Мягко удаляет прогон.</summary>
    public Task<bool> SoftDeleteAsync(Guid runId, CancellationToken ct = default);

    public Task<IReadOnlyList<BenchmarkRun>> GetRunsAsync(string? caseId = null, Guid? modelId = null,
        ProgressStatus? status = null, LlmReasoningEffort? reasoning = null, int limit = 50, int offset = 0,
        CancellationToken ct = default);

    /// <summary>Возвращает прогон с результатами; удалённый — только при <paramref name="includeDeleted"/>.</summary>
    public Task<BenchmarkRun?> GetRunByIdAsync(Guid runId, bool includeDeleted = false,
        CancellationToken ct = default);

    /// <summary>Агрегирует все неудалённые прогоны по тройке (тест, модель, уровень рассуждений).</summary>
    public Task<IReadOnlyList<BenchmarkSummary>> GetSummaryAsync(string? caseId = null, Guid? modelId = null,
        CancellationToken ct = default);
}
