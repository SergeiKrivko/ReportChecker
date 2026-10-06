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
        return await CreateClientAsync(modelId, type, report.Id);
    }

    public async Task<IAiAgent> CreateClientAsync(Guid? modelId, LlmUsageType type, Guid? reportId = null,
        CancellationToken ct = default)
    {
        LlmModel? model = null;
        if (modelId.HasValue)
        {
            model = await llmModelRepository.GetModelByIdAsync(modelId.Value, ct);
            if (model == null)
                logger.LogWarning("Model '{id}' not found. Use default model instead", modelId.Value);
        }

        model ??= await llmModelRepository.GetDefaultModelAsync(ct);

        var apiKey = configuration["Ai.ApiKey"] ?? throw new Exception("API key not found");
        var client = new ChatClient(model.ModelKey, new ApiKeyCredential(apiKey), new OpenAIClientOptions
        {
            Endpoint = new Uri(configuration["Ai.ApiUrl"] ?? throw new Exception("AI API url not set")),
        });

        // Guid.Empty означает «без отчёта»: расход не пишется в LlmUsages (например, бенчмарк).
        return new AiAgent(client, type, model, reportId ?? Guid.Empty, llmUsageRepository, logger);
    }
}
