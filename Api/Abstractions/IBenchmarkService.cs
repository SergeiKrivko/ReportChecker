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
        CancellationToken ct = default);

    public Task<IReadOnlyList<BenchmarkRun>> GetRunsAsync(string? caseId = null, Guid? modelId = null,
        ProgressStatus? status = null, int limit = 50, int offset = 0, CancellationToken ct = default);

    public Task<BenchmarkRun?> GetRunAsync(Guid runId, CancellationToken ct = default);

    public Task<IReadOnlyList<BenchmarkSummary>> GetSummaryAsync(string? caseId = null, Guid? modelId = null,
        CancellationToken ct = default);

    /// <summary>Отменяет активный прогон и мягко удаляет его.</summary>
    public Task<bool> DeleteRunAsync(Guid runId, CancellationToken ct = default);
}
