using ReportChecker.Api.Dtos;
using ReportChecker.Models;

namespace ReportChecker.Api.Converters;

public static class BenchmarkRunConverter
{
    public static BenchmarkRunDto ToDto(this BenchmarkRun run)
    {
        return new BenchmarkRunDto
        {
            Id = run.Id,
            CaseId = run.CaseId,
            CaseName = run.CaseName,
            ModelId = run.ModelId,
            ModelDisplayName = run.ModelDisplayName,
            ReasoningEffort = run.ReasoningEffort,
            Status = run.Status,
            CreatedAt = run.CreatedAt,
            StartedAt = run.StartedAt,
            FinishedAt = run.FinishedAt,
            DeletedAt = run.DeletedAt,
            FailureReason = run.FailureReason,
            ExpectedCount = run.ExpectedCount,
            FoundCount = run.FoundCount,
            MatchedCount = run.MatchedCount,
            TitleMatchCount = run.TitleMatchCount,
            PriorityMatchCount = run.PriorityMatchCount,
            FixCheckedCount = run.FixCheckedCount,
            FixMatchCount = run.FixMatchCount,
            InputTokens = run.InputTokens,
            OutputTokens = run.OutputTokens,
            TotalTokens = run.TotalTokens,
            TotalRequests = run.TotalRequests,
            TotalCost = run.TotalCost,
            Results = run.Results.Select(e => e.ToDto()).ToArray(),
        };
    }

    public static BenchmarkResultDto ToDto(this BenchmarkResult result)
    {
        return new BenchmarkResultDto
        {
            Id = result.Id,
            RunId = result.RunId,
            CreatedAt = result.CreatedAt,
            ExpectedNumber = result.ExpectedNumber,
            ErrorClass = result.ErrorClass,
            Chapter = result.Chapter,
            Line = result.Line,
            ExpectedTitle = result.ExpectedTitle,
            ExpectedComment = result.ExpectedComment,
            ExpectedPriority = result.ExpectedPriority,
            FoundIndex = result.FoundIndex,
            FoundTitle = result.FoundTitle,
            FoundComment = result.FoundComment,
            FoundPriority = result.FoundPriority,
            IsFound = result.IsFound,
            TitleMatch = result.TitleMatch,
            PriorityMatch = result.PriorityMatch,
            PriorityDelta = result.PriorityDelta,
            MatchingMethod = result.MatchingMethod,
            MatchScore = result.MatchScore,
            MatchingReason = result.MatchingReason,
            FixMatchStatus = result.FixMatchStatus,
            ExpectedFix = result.ExpectedFix,
            FoundFix = result.FoundFix,
        };
    }
}