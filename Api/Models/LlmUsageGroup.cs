namespace ReportChecker.Models;

public class LlmUsageGroup
{
    public required Guid ReportId { get; init; }
    public required Guid ModelId { get; init; }
    public int TotalTokens { get; init; }
    public int TotalRequests { get; init; }
}