using ReportChecker.Models;

namespace AiAgent;

public interface IAiAgentFactory
{
    public Task<IAiAgent> CreateClientAsync(Report report, LlmUsageType type);

    /// <summary>
    /// Создаёт клиент без привязки к отчёту (используется бенчмарком: подписка и лимиты не проверяются).
    /// Расход записывается в <c>LlmUsages</c> только если задан <paramref name="reportId"/>.
    /// </summary>
    public Task<IAiAgent> CreateClientAsync(Guid? modelId, LlmUsageType type, Guid? reportId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Создаёт клиент без привязки к отчёту (используется бенчмарком: подписка и лимиты не проверяются).
    /// Расход записывается в <c>LlmUsages</c> только если задан <paramref name="reportId"/>.
    /// </summary>
    public Task<IAiAgent> CreateClientAsync(Guid? modelId, LlmUsageType type,
        LlmReasoningEffort reasoningEffort = LlmReasoningEffort.None, Guid? reportId = null,
        CancellationToken ct = default);
}