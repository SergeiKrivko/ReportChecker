using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Converters;
using ReportChecker.DataAccess.Entities;
using ReportChecker.DataAccess.Extensions;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Repositories;

public class LlmUsageRepository(ReportCheckerDbContext dbContext) : ILlmUsageRepository
{
    public async Task<IReadOnlyList<LlmUsage>> GetAllUsagesForReportAsync(Guid reportId, CancellationToken ct = default)
    {
        var entities = await dbContext.LlmUsages
            .Where(e => e.ReportId == reportId)
            .ToListAsync(ct);
        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<LlmUsage>> GetAllUsagesOfModelAsync(Guid modelId, CancellationToken ct = default)
    {
        var entities = await dbContext.LlmUsages
            .Where(e => e.ModelId == modelId)
            .ToListAsync(ct);
        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task<int> GetTotalUsageAsync(Guid userId, DateTime startsAt, CancellationToken ct = default)
    {
        var total = await dbContext.LlmUsages
            .Where(e => e.FinishedAt > startsAt)
            .Include(e => e.Report)
            .Where(e => e.Report.OwnerId == userId)
            .SumAsync(e => e.TotalTokens, ct);
        return total;
    }

    public async Task<Guid> CreateUsageAsync(LlmUsage usage, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        var entity = new LlmUsageEntity
        {
            Id = id,
            ModelId = usage.ModelId,
            ReportId = usage.ReportId,
            Type = usage.Type,
            FinishedAt = usage.FinishedAt,
            InputTokens = usage.InputTokens,
            OutputTokens = usage.OutputTokens,
            TotalTokens = usage.TotalTokens,
            TotalRequests = usage.TotalRequests,
        };
        await dbContext.LlmUsages.AddAsync(entity, ct);
        await dbContext.SaveChangesAsync(ct);
        return id;
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetModelsUsageAsync(Guid userId, DateTime timeFrom,
        DateTime timeTo, CancellationToken ct = default)
    {
        return await dbContext.LlmUsages
            .Where(e => e.FinishedAt > timeFrom && e.FinishedAt < timeTo)
            .Include(e => e.Report)
            .Where(e => e.Report.OwnerId == userId)
            .GroupBy(e => e.ModelId)
            .ToDictionaryAsync(e => e.Key, e => e.Sum(u => u.TotalTokens), ct);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetReportsUsageAsync(Guid userId, DateTime timeFrom,
        DateTime timeTo, CancellationToken ct = default)
    {
        return await dbContext.LlmUsages
            .Where(e => e.FinishedAt > timeFrom && e.FinishedAt < timeTo)
            .Include(e => e.Report)
            .Where(e => e.Report.OwnerId == userId)
            .GroupBy(e => e.ReportId)
            .ToDictionaryAsync(e => e.Key, e => e.Sum(u => u.TotalTokens), ct);
    }

    public async Task<IReadOnlyList<LlmUsageGroup>> GetUsageStatisticsAsync(Guid userId, DateTime timeFrom,
        DateTime timeTo, CancellationToken ct = default)
    {
        var entities = await dbContext.LlmUsages
            .Where(e => e.FinishedAt > timeFrom && e.FinishedAt < timeTo)
            .Include(e => e.Report)
            .Where(e => e.Report.OwnerId == userId)
            .GroupBy(e => e.ModelId)
            .Select(e => e
                .GroupBy(a => a.ReportId)
                .Select(a => new
                {
                    ModelId = e.Key,
                    ReportId = a.Key,
                    TotalTokens = a.Sum(g => g.TotalTokens),
                    TotalRequests = a.Sum(g => g.TotalRequests),
                }))
            .ToListAsync(ct);
        return entities.SelectMany(l => l.Select(e => new LlmUsageGroup
        {
            ModelId = e.ModelId,
            ReportId = e.ReportId,
            TotalTokens = e.TotalTokens,
            TotalRequests = e.TotalRequests,
        })).ToList();
    }

    public async Task<IReadOnlyList<LlmUsageInterval>> GetTimeUsageAsync(Guid userId, DateTime timeFrom,
        DateTime timeTo, Guid? modelId = null, Guid? reportId = null, int? numberOfIntervals = null,
        CancellationToken ct = default)
    {
        var query = dbContext.LlmUsages
            .Where(e => e.FinishedAt > timeFrom && e.FinishedAt < timeTo)
            .Include(e => e.Report)
            .Where(e => e.Report.OwnerId == userId);

        if (modelId != null)
            query = query.Where(e => e.ModelId == modelId.Value);
        if (reportId != null)
            query = query.Where(e => e.ReportId == reportId.Value);

        numberOfIntervals ??= (int)(timeTo - timeFrom).TotalDays;
        var entities = await query.ToListAsync(ct);
        return entities.GroupByIntervals(e => e.FinishedAt, timeFrom, timeTo, numberOfIntervals.Value)
            .Select(e => new LlmUsageInterval
            {
                StartTime = e.IntervalStart,
                EndTime = e.IntervalEnd,
                TotalTokens = e.Items.Sum(u => u.TotalTokens),
                TotalRequests = e.Items.Sum(u => u.TotalRequests),
            })
            .ToList();
    }
}