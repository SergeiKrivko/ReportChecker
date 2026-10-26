using ReportChecker.Models;

namespace ReportChecker.Api.Dtos;

public class BenchmarkSummaryDto
{
    public required string CaseId { get; init; }

    public required Guid ModelId { get; init; }

    public LlmReasoningEffort ReasoningEffort { get; init; }

    public string? CaseName { get; init; }

    public string? ModelDisplayName { get; init; }

    public int RunCount { get; init; }

    public int CompletedRunCount { get; init; }

    public int FailedRunCount { get; init; }

    public int CancelledRunCount { get; init; }

    public long? AvgDurationMs { get; init; }

    public long? MinDurationMs { get; init; }

    public long? MaxDurationMs { get; init; }

    public DateTime? LastRunAt { get; init; }

    public int TotalExpected { get; init; }

    public int TotalFound { get; init; }

    public int TotalMatched { get; init; }

    public int TotalExtra { get; init; }

    public int TotalTitleMatched { get; init; }
    public int TotalPriorityMatched { get; init; }
    public int TotalFixChecked { get; init; }
    public int TotalFixMatched { get; init; }

    public int TotalInputTokens { get; init; }
    public int TotalOutputTokens { get; init; }
    public int TotalTokens { get; init; }
    public int TotalRequests { get; init; }
    public decimal TotalCost { get; init; }

    public double MatchedShare => TotalExpected == 0 ? 0 : (double)TotalMatched / TotalExpected;

    public double TitleMatchShare => TotalMatched == 0 ? 0 : (double)TotalTitleMatched / TotalMatched;

    public double PriorityMatchShare => TotalMatched == 0 ? 0 : (double)TotalPriorityMatched / TotalMatched;

    public double FixMatchShare => TotalFixChecked == 0 ? 0 : (double)TotalFixMatched / TotalFixChecked;
}