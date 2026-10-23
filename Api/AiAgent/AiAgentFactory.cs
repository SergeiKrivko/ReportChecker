using System.ClientModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using ReportChecker.Abstractions;
using ReportChecker.Models;

namespace AiAgent;

public class AiAgentFactory(
    ILlmModelRepository llmModelRepository,
    ILlmUsageRepository llmUsageRepository,
    ISubscriptionService subscriptionService,
    IConfiguration configuration,
    ILogger<AiAgent> logger) : IAiAgentFactory
{
    public async Task<IAiAgent> CreateClientAsync(Report report, LlmUsageType type)
    {
        if (!await subscriptionService.CheckTokensLimitAsync(report.OwnerId))
            throw new Exception("Tokens limit reached");

        // Без активной подписки модель отчёта игнорируется — используется модель по умолчанию.
        var subscription = await subscriptionService.GetActiveSubscription(report.OwnerId);
        var modelId = subscription == null ? null : report.LlmModelId;
        var model = await GetModelAsync(modelId, CancellationToken.None);

        // Приоритет у настройки отчёта; Default означает «взять из модели».
        var reasoningLevel = report.ReasoningLevel == LlmReasoningEffort.Default
            ? model.DefaultReasoningLevel
            : report.ReasoningLevel;

        return CreateAgent(model, type, reasoningLevel, report.Id);
    }

    public async Task<IAiAgent> CreateClientAsync(Guid? modelId, LlmUsageType type, Guid? reportId = null,
        CancellationToken ct = default)
    {
        return await CreateClientAsync(modelId, type, LlmReasoningEffort.None, reportId, ct);
    }

    public async Task<IAiAgent> CreateClientAsync(Guid? modelId, LlmUsageType type, LlmReasoningEffort reasoningEffort = LlmReasoningEffort.None,
        Guid? reportId = null, CancellationToken ct = default)
    {
        var model = await GetModelAsync(modelId, ct);
        return CreateAgent(model, type, reasoningEffort, reportId ?? Guid.Empty);
    }

    private async Task<LlmModel> GetModelAsync(Guid? modelId, CancellationToken ct)
    {
        LlmModel? model = null;
        if (modelId.HasValue)
        {
            model = await llmModelRepository.GetModelByIdAsync(modelId.Value, ct);
            if (model == null)
                logger.LogWarning("Model '{id}' not found. Use default model instead", modelId.Value);
        }

        return model ?? await llmModelRepository.GetDefaultModelAsync(ct);
    }

    private IAiAgent CreateAgent(LlmModel model, LlmUsageType type, LlmReasoningEffort reasoningEffort, Guid reportId)
    {
        var apiKey = configuration["Ai.ApiKey"] ?? throw new Exception("API key not found");
        var client = new ChatClient(model.ModelKey, new ApiKeyCredential(apiKey), new OpenAIClientOptions
        {
            Endpoint = new Uri(configuration["Ai.ApiUrl"] ?? throw new Exception("AI API url not set")),
            NetworkTimeout = TimeSpan.FromMinutes(5),
        });

        // Guid.Empty означает «без отчёта»: расход не пишется в LlmUsages (например, бенчмарк).
        return new AiAgent(client, type, model, reportId, reasoningEffort, llmUsageRepository, logger);
    }
}
