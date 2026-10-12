using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IBenchmarkService
{
    /// <summary>
    /// Создаёт прогоны (по одному на пару тест–модель) и запускает их в фоне.
    /// </summary>
    public Task<IReadOnlyList<Guid>> CreateRunsAsync(BenchmarkRunRequest request, CancellationToken ct = default);

    /// <summary>Выполняет один прогон до конца; вызывается из фонового задания.</summary>
    public Task RunAsync(Guid runId, string caseId, Guid modelId, bool? useLlmMatching,
        LlmReasoningEffort? reasoningEffort = null, CancellationToken ct = default);

    /// <summary>
    /// Прогоны, отсортированные по времени создания (сначала новые). Мягко удалённые
    /// попадают в выборку только при <paramref name="includeDeleted"/>.
    /// </summary>
    public Task<IReadOnlyList<BenchmarkRun>> GetRunsAsync(string? caseId = null, Guid? modelId = null,
        ProgressStatus? status = null, LlmReasoningEffort? reasoning = null, int limit = 50, int offset = 0,
        bool includeDeleted = false, CancellationToken ct = default);

    /// <summary>
    /// Один прогон со всеми строками результатов. Удалённый отдаётся только при
    /// <paramref name="includeDeleted"/>; в ответе он помечен <see cref="BenchmarkRun.DeletedAt"/>.
    /// </summary>
    public Task<BenchmarkRun?> GetRunAsync(Guid runId, bool includeDeleted = false,
        CancellationToken ct = default);

    public Task<IReadOnlyList<BenchmarkSummary>> GetSummaryAsync(string? caseId = null, Guid? modelId = null,
        CancellationToken ct = default);

    /// <summary>Отменяет активный прогон и мягко удаляет его.</summary>
    public Task<bool> DeleteRunAsync(Guid runId, CancellationToken ct = default);
}
