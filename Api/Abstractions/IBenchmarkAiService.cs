using ReportChecker.Models;

namespace ReportChecker.Abstractions;

/// <summary>
/// Поиск и сопоставление ошибок выбранной моделью для бенчмарка.
/// </summary>
public interface IBenchmarkAiService
{
    /// <summary>Ищет ошибки в главах теста тем же агентом, что и первая проверка отчёта.</summary>
    public Task<BenchmarkFindResult> FindIssuesAsync(IReadOnlyList<Chapter> chapters, Guid modelId,
        LlmReasoningEffort? reasoningEffort = null, CancellationToken ct = default);

    /// <summary>Сопоставляет оставшиеся найденные ошибки с известными одним LLM-запросом.</summary>
    public Task<BenchmarkMatchResult> MatchIssuesAsync(BenchmarkMatchRequest request, Guid modelId,
        CancellationToken ct = default);
}
