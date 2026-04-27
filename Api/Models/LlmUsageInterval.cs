namespace ReportChecker.Models;

public class LlmUsageInterval
{
    public required DateTime StartTime { get; init; }
    public required DateTime EndTime { get; init; }
    public required int TotalTokens { get; init; }
    public required int TotalRequests { get; init; }
}