using Avalux.OpenAi.Client;
using Microsoft.Extensions.Logging;
using ReportChecker.Abstractions;
using ReportChecker.Models;

namespace AiAgent;

public class AiAgent : AiAgentClientBase<string>
{
    private readonly ILogger _logger;
    private readonly OpenAiClient<string> _client;
    private readonly ILlmUsageRepository _llmUsageRepository;
    private readonly LlmUsageType _type;
    private readonly LlmModel _model;
    private readonly Guid _reportId;

    internal AiAgent(OpenAiClient<string> client, LlmUsageType type, LlmModel model, Guid reportId,
        ILlmUsageRepository llmUsageRepository, ILogger<AiAgent> logger) :
        base(client)
    {
        _logger = logger;
        _client = client;
        _llmUsageRepository = llmUsageRepository;

        _type = type;
        _model = model;
        _reportId = reportId;
    }

    public override async ValueTask DisposeAsync()
    {
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Disposing AI agent... Total requests: {totalRequests}",
                _client.Usage.TotalRequests);
        await _llmUsageRepository.CreateUsageAsync(new LlmUsage
        {
            ReportId = _reportId,
            ModelId = _model.Id,
            FinishedAt = DateTime.UtcNow,
            Type = _type,

            InputTokens = _client.Usage.InputTokens,
            OutputTokens = _client.Usage.OutputTokens,
            TotalTokens = (int)(_client.Usage.InputTokens * _model.InputCoefficient +
                                _client.Usage.OutputTokens * _model.OutputCoefficient),
            TotalRequests = _client.Usage.TotalRequests,
        });
    }
}