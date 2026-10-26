using ReportChecker.Api.Dtos;
using ReportChecker.Models;

namespace ReportChecker.Api.Converters;

public static class BenchmarkSummaryConverter
{
    public static BenchmarkSummaryDto ToDto(this BenchmarkSummary summary)
    {
        return new BenchmarkSummaryDto
        {
            CaseId = summary.CaseId,
            ModelId = summary.ModelId,
            ReasoningEffort = summary.ReasoningEffort,
            CaseName = summary.CaseName,
            ModelDisplayName = summary.ModelDisplayName,
            RunCount = summary.RunCount,
            CompletedRunCount = summary.CompletedRunCount,
            FailedRunCount = summary.FailedRunCount,
            CancelledRunCount = summary.CancelledRunCount,
            AvgDurationMs = summary.AvgDurationMs,
            MinDurationMs = summary.MinDurationMs,
            MaxDurationMs = summary.MaxDurationMs,
            LastRunAt = summary.LastRunAt,
            TotalExpected = summary.TotalExpected,
            TotalFound = summary.TotalFound,
            TotalMatched = summary.TotalMatched,
            TotalExtra = summary.TotalExtra,
            TotalTitleMatched = summary.TotalTitleMatched,
            TotalPriorityMatched = summary.TotalPriorityMatched,
            TotalFixChecked = summary.TotalFixChecked,
            TotalFixMatched = summary.TotalFixMatched,
            TotalInputTokens = summary.TotalInputTokens,
            TotalOutputTokens = summary.TotalOutputTokens,
            TotalTokens = summary.TotalTokens,
            TotalRequests = summary.TotalRequests,
            TotalCost = summary.TotalCost,
        };
    }
}