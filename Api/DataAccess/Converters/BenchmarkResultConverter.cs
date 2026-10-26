using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Converters;

public static class BenchmarkResultConverter
{
    public static BenchmarkResult ToDomain(this BenchmarkResultEntity entity)
    {
        return new BenchmarkResult
        {
            Id = entity.Id,
            RunId = entity.RunId,
            CreatedAt = entity.CreatedAt,
            ExpectedNumber = entity.ExpectedNumber,
            ErrorClass = entity.ErrorClass,
            Chapter = entity.Chapter,
            Line = entity.Line,
            ExpectedTitle = entity.ExpectedTitle,
            ExpectedComment = entity.ExpectedComment,
            ExpectedPriority = entity.ExpectedPriority,
            FoundIndex = entity.FoundIndex,
            FoundTitle = entity.FoundTitle,
            FoundComment = entity.FoundComment,
            FoundPriority = entity.FoundPriority,
            IsFound = entity.IsFound,
            TitleMatch = entity.TitleMatch,
            PriorityMatch = entity.PriorityMatch,
            PriorityDelta = entity.PriorityDelta,
            MatchingMethod = entity.MatchingMethod,
            MatchScore = entity.MatchScore,
            MatchingReason = entity.MatchingReason,
            FixMatchStatus = entity.FixMatchStatus,
            ExpectedFix = entity.ExpectedFix,
            FoundFix = entity.FoundFix,
        };
    }

    public static BenchmarkResultEntity ToEntity(this BenchmarkResult result, Guid runId, DateTime createdAt)
    {
        return new BenchmarkResultEntity
        {
            Id = result.Id == Guid.Empty ? Guid.NewGuid() : result.Id,
            RunId = runId,
            CreatedAt = createdAt,
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