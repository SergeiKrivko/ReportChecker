using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Converters;
using ReportChecker.DataAccess.Entities;
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
}