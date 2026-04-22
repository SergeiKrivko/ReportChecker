using ReportChecker.Models;

namespace AiAgent;

public interface IAiAgentFactory
{
    public Task<IAiAgentClient<string>> CreateClientAsync(Report report, LlmUsageType type);
}