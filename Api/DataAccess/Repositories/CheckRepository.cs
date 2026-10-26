using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Converters;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Repositories;

public class CheckRepository(ReportCheckerDbContext dbContext) : ICheckRepository
{
    public async Task<Guid> CreateCheckAsync(Guid reportId, Guid userId, string? name = null,
        CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        var entity = new CheckEntity
        {
            CheckId = id,
            ReportId = reportId,
            UserId = userId,
            Name = name,
            CreatedAt = DateTime.UtcNow,
        };
        await dbContext.Checks.AddAsync(entity, ct);
        await dbContext.SaveChangesAsync(ct);
        return id;
    }

    public async Task<Check?> GetCheckByIdAsync(Guid checkId, CancellationToken ct = default)
    {
        var result = await dbContext.Checks
            .Where(e => e.CheckId == checkId)
            .FirstOrDefaultAsync(ct);
        return result?.ToDomain();
    }

    public async Task<Check?> GetPreviousCheckAsync(Check offset, CancellationToken ct = default)
    {
        var result = await dbContext.Checks
            .Where(e => e.ReportId == offset.ReportId && e.CreatedAt < offset.CreatedAt)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync(ct);
        return result?.ToDomain();
    }

    public async Task<Check?> GetLatestCheckOfReportAsync(Guid reportId, bool includeFailed = false,
        CancellationToken ct = default)
    {
        var result = await (includeFailed
                ? dbContext.Checks
                    .Where(e => e.ReportId == reportId)
                : dbContext.Checks
                    .Where(e => e.ReportId == reportId && e.Status != ProgressStatus.Failed &&
                                e.Status != ProgressStatus.Cancelled))
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync(ct);
        return result?.ToDomain();
    }

    public async Task<IEnumerable<Check>> GetAllChecksOfReportAsync(Guid reportId, CancellationToken ct = default)
    {
        var result = await dbContext.Checks
            .Where(e => e.ReportId == reportId)
            .ToListAsync(ct);
        return result.Select(e => e.ToDomain());
    }

    public async Task SetCheckStatusAsync(Guid checkId, ProgressStatus status, CancellationToken ct = default)
    {
        await dbContext.Checks
            .Where(e => e.CheckId == checkId)
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.Status, status), ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<int> CountChecksAsync(Guid userId, DateTime startDate, CancellationToken ct = default)
    {
        return await dbContext.Checks
            .Where(e => e.CreatedAt > startDate)
            .Include(e => e.Report)
            .Where(e => e.Report.OwnerId == userId)
            .CountAsync(ct);
    }
}