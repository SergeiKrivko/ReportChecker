using Avalux.OpenAi.Client;
using Avalux.OpenAi.Client.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        LlmModel? model = null;
        if (report.LlmModelId.HasValue)
        {
            model = await llmModelRepository.GetModelByIdAsync(report.LlmModelId.Value);
            if (model == null)
                logger.LogWarning("Model '{id}' not found. Use default model instead", report.LlmModelId.Value);
        }

        if (!await subscriptionService.CheckTokensLimitAsync(report.OwnerId))
            throw new Exception("Tokens limit reached");

        var subscription = await subscriptionService.GetActiveSubscription(report.OwnerId);
        if (subscription == null || model == null)
            model = await llmModelRepository.GetDefaultModelAsync();

        var client = new OpenAiClient<string>(new OpenAiClientOptions
        {
            ApiEndpoint = new Uri(configuration["Ai.ApiUrl"] ?? throw new Exception("AI API url not set")),
            ApiKey = configuration["Ai.ApiKey"] ?? "no key",
            Model = model.ModelKey,
        });

        return new AiAgent(client, type, model, report.Id, llmUsageRepository, logger);
    }
}