using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Converters;

public static class BenchmarkRunConverter
{
    public static BenchmarkRun ToDomain(this BenchmarkRunEntity entity,
        IEnumerable<BenchmarkResultEntity>? results = null)
    {
        return new BenchmarkRun
        {
            Id = entity.Id,
            CaseId = entity.CaseId,
            CaseName = entity.CaseName,
            ModelId = entity.ModelId,
            ModelDisplayName = entity.Model?.DisplayName,
            ReasoningEffort = entity.ReasoningEffort,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt,
            StartedAt = entity.StartedAt,
            FinishedAt = entity.FinishedAt,
            DeletedAt = entity.DeletedAt,
            FailureReason = entity.FailureReason,
            ExpectedCount = entity.ExpectedCount,
            FoundCount = entity.FoundCount,
            MatchedCount = entity.MatchedCount,
            TitleMatchCount = entity.TitleMatchCount,
            PriorityMatchCount = entity.PriorityMatchCount,
            FixCheckedCount = entity.FixCheckedCount,
            FixMatchCount = entity.FixMatchCount,
            InputTokens = entity.InputTokens,
            OutputTokens = entity.OutputTokens,
            TotalTokens = entity.TotalTokens,
            TotalRequests = entity.TotalRequests,
            TotalCost = entity.TotalCost,
            Results = results?.Select(e => e.ToDomain()).ToArray() ?? [],
        };
    }
}