using ReportChecker.Models;

namespace AiAgent;

public interface IAiAgentFactory
{
    public Task<IAiAgent> CreateClientAsync(Report report, LlmUsageType type);
}