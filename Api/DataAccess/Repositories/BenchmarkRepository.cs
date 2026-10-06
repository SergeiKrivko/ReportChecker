using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Repositories;

public class BenchmarkRepository(ReportCheckerDbContext dbContext) : IBenchmarkRepository
{
    public async Task<Guid> CreateRunAsync(string caseId, string caseName, Guid modelId,
        CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        var entity = new BenchmarkRunEntity
        {
            Id = id,
            CaseId = caseId,
            CaseName = caseName,
            ModelId = modelId,
            Status = ProgressStatus.Queued,
            CreatedAt = DateTime.UtcNow,
        };
        await dbContext.BenchmarkRuns.AddAsync(entity, ct);
        await dbContext.SaveChangesAsync(ct);
        return id;
    }

    public async Task<bool> SetStatusAsync(Guid runId, ProgressStatus status, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var entity = await dbContext.BenchmarkRuns
            .AsNoTracking()
            .Where(e => e.Id == runId)
            .Select(e => new { e.StartedAt, e.FinishedAt })
            .FirstOrDefaultAsync(ct);
        if (entity == null)
            return false;

        var startedAt = status == ProgressStatus.InProgress ? entity.StartedAt ?? now : entity.StartedAt;
        var finishedAt = IsFinished(status) ? entity.FinishedAt ?? now : entity.FinishedAt;

        await dbContext.BenchmarkRuns
            .Where(e => e.Id == runId)
            .ExecuteUpdateAsync(p => p
                .SetProperty(e => e.Status, status)
                .SetProperty(e => e.StartedAt, startedAt)
                .SetProperty(e => e.FinishedAt, finishedAt), ct);
        return true;
    }

    public async Task<bool> CompleteRunAsync(Guid runId, IReadOnlyCollection<BenchmarkResult> results,
        BenchmarkRun aggregates, BenchmarkUsage usage, DateTime finishedAt, CancellationToken ct = default)
    {
        var count = await dbContext.BenchmarkRuns
            .Where(e => e.Id == runId)
            .ExecuteUpdateAsync(p => p
                .SetProperty(e => e.Status, ProgressStatus.Completed)
                .SetProperty(e => e.FinishedAt, finishedAt)
                .SetProperty(e => e.FailureReason, (string?)null)
                .SetProperty(e => e.ExpectedCount, aggregates.ExpectedCount)
                .SetProperty(e => e.FoundCount, aggregates.FoundCount)
                .SetProperty(e => e.MatchedCount, aggregates.MatchedCount)
                .SetProperty(e => e.TitleMatchCount, aggregates.TitleMatchCount)
                .SetProperty(e => e.PriorityMatchCount, aggregates.PriorityMatchCount)
                .SetProperty(e => e.FixCheckedCount, aggregates.FixCheckedCount)
                .SetProperty(e => e.FixMatchCount, aggregates.FixMatchCount)
                .SetProperty(e => e.InputTokens, usage.InputTokens)
                .SetProperty(e => e.OutputTokens, usage.OutputTokens)
                .SetProperty(e => e.TotalTokens, usage.TotalTokens)
                .SetProperty(e => e.TotalRequests, usage.TotalRequests)
                .SetProperty(e => e.TotalCost, usage.TotalCost), ct);
        if (count == 0)
            return false;

        var createdAt = DateTime.UtcNow;
        foreach (var result in results)
        {
            var resultEntity = FromDomain(result, runId, createdAt);
            await dbContext.BenchmarkResults.AddAsync(resultEntity, ct);
        }

        await dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> FailRunAsync(Guid runId, string reason, DateTime finishedAt,
        CancellationToken ct = default)
    {
        var failureReason = reason.Length > 2000 ? reason[..2000] : reason;
        var count = await dbContext.BenchmarkRuns
            .Where(e => e.Id == runId)
            .ExecuteUpdateAsync(p => p
                .SetProperty(e => e.Status, ProgressStatus.Failed)
                .SetProperty(e => e.FinishedAt, e => e.FinishedAt ?? finishedAt)
                .SetProperty(e => e.FailureReason, failureReason), ct);
        return count > 0;
    }

    public async Task<bool> SoftDeleteAsync(Guid runId, CancellationToken ct = default)
    {
        var count = await dbContext.BenchmarkRuns
            .Where(e => e.Id == runId && e.DeletedAt == null)
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.DeletedAt, DateTime.UtcNow), ct);
        return count > 0;
    }

    public async Task<IReadOnlyList<BenchmarkRun>> GetRunsAsync(string? caseId = null, Guid? modelId = null,
        ProgressStatus? status = null, int limit = 50, int offset = 0, CancellationToken ct = default)
    {
        var entities = await FilteredRuns(caseId, modelId, status)
            .Include(e => e.Model)
            .OrderByDescending(e => e.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);
        return entities.Select(e => FromEntity(e)).ToList();
    }

    public async Task<BenchmarkRun?> GetRunByIdAsync(Guid runId, bool includeDeleted = false,
        CancellationToken ct = default)
    {
        var query = dbContext.BenchmarkRuns.AsNoTracking();
        if (!includeDeleted)
            query = query.Where(e => e.DeletedAt == null);

        var entity = await query
            .Where(e => e.Id == runId)
            .Include(e => e.Results)
            .Include(e => e.Model)
            .FirstOrDefaultAsync(ct);
        return entity == null ? null : FromEntity(entity, entity.Results);
    }

    public async Task<IReadOnlyList<BenchmarkSummary>> GetSummaryAsync(string? caseId = null, Guid? modelId = null,
        CancellationToken ct = default)
    {
        var runs = await FilteredRuns(caseId, modelId, null)
            .Include(e => e.Model)
            .ToListAsync(ct);

        return runs
            .GroupBy(e => new { e.CaseId, e.ModelId })
            .Select(g => BuildSummary(g.Key.CaseId, g.Key.ModelId,
                g.Select(e => e.Model?.DisplayName).FirstOrDefault(e => e != null), g.ToList()))
            .OrderBy(e => e.CaseId)
            .ThenBy(e => e.ModelDisplayName)
            .ToList();
    }

    private static BenchmarkSummary BuildSummary(string caseId, Guid modelId, string? modelDisplayName,
        IReadOnlyList<BenchmarkRunEntity> runs)
    {
        var durations = runs
            .Where(e => e.StartedAt != null && e.FinishedAt != null)
            .Select(e => (long)(e.FinishedAt!.Value - e.StartedAt!.Value).TotalMilliseconds)
            .ToList();

        return new BenchmarkSummary
        {
            CaseId = caseId,
            ModelId = modelId,
            CaseName = runs[0].CaseName,
            ModelDisplayName = modelDisplayName,
            RunCount = runs.Count,
            CompletedRunCount = runs.Count(e => e.Status == ProgressStatus.Completed),
            FailedRunCount = runs.Count(e => e.Status == ProgressStatus.Failed),
            CancelledRunCount = runs.Count(e => e.Status == ProgressStatus.Cancelled),
            AvgDurationMs = durations.Count == 0 ? null : (long)durations.Average(),
            MinDurationMs = durations.Count == 0 ? null : durations.Min(),
            MaxDurationMs = durations.Count == 0 ? null : durations.Max(),
            LastRunAt = runs.Max(e => e.CreatedAt),
            TotalExpected = runs.Sum(e => e.ExpectedCount),
            TotalFound = runs.Sum(e => e.FoundCount),
            TotalMatched = runs.Sum(e => e.MatchedCount),
            TotalExtra = runs.Sum(e => e.FoundCount - e.MatchedCount),
            TotalTitleMatched = runs.Sum(e => e.TitleMatchCount),
            TotalPriorityMatched = runs.Sum(e => e.PriorityMatchCount),
            TotalFixChecked = runs.Sum(e => e.FixCheckedCount),
            TotalFixMatched = runs.Sum(e => e.FixMatchCount),
            TotalInputTokens = runs.Sum(e => e.InputTokens),
            TotalOutputTokens = runs.Sum(e => e.OutputTokens),
            TotalTokens = runs.Sum(e => e.TotalTokens),
            TotalRequests = runs.Sum(e => e.TotalRequests),
            TotalCost = runs.Sum(e => e.TotalCost),
        };
    }

    private IQueryable<BenchmarkRunEntity> FilteredRuns(string? caseId, Guid? modelId, ProgressStatus? status)
    {
        var query = dbContext.BenchmarkRuns
            .AsNoTracking()
            .Where(e => e.DeletedAt == null);
        if (!string.IsNullOrWhiteSpace(caseId))
            query = query.Where(e => e.CaseId == caseId);
        if (modelId != null)
            query = query.Where(e => e.ModelId == modelId.Value);
        if (status != null)
            query = query.Where(e => e.Status == status.Value);
        return query;
    }

    private static bool IsFinished(ProgressStatus status)
    {
        return status is ProgressStatus.Completed or ProgressStatus.Failed or ProgressStatus.Cancelled;
    }

    private static BenchmarkResultEntity FromDomain(BenchmarkResult result, Guid runId, DateTime createdAt)
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

    private static BenchmarkRun FromEntity(BenchmarkRunEntity entity,
        IEnumerable<BenchmarkResultEntity>? results = null)
    {
        return new BenchmarkRun
        {
            Id = entity.Id,
            CaseId = entity.CaseId,
            CaseName = entity.CaseName,
            ModelId = entity.ModelId,
            ModelDisplayName = entity.Model?.DisplayName,
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
            Results = results?.Select(FromEntity).ToArray() ?? [],
        };
    }

    private static BenchmarkResult FromEntity(BenchmarkResultEntity entity)
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
}
