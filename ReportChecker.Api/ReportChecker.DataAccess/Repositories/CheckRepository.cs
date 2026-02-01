using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Repositories;

public class CheckRepository(ReportCheckerDbContext dbContext) : ICheckRepository
{
    public async Task<Guid> CreateCheckAsync(Guid reportId, Guid userId, string? source = null, string? name = null)
    {
        var id = Guid.NewGuid();
        var entity = new CheckEntity
        {
            CheckId = id,
            ReportId = reportId,
            UserId = userId,
            Source = source,
            Name = name,
            CreatedAt = DateTime.UtcNow,
        };
        await dbContext.Checks.AddAsync(entity);
        await dbContext.SaveChangesAsync();
        return id;
    }

    public async Task<Check?> GetCheckByIdAsync(Guid checkId)
    {
        var result = await dbContext.Checks
            .Where(e => e.CheckId == checkId)
            .FirstOrDefaultAsync();
        return result is null ? null : FromEntity(result);
    }

    public async Task<Check?> GetLatestCheckOfReportAsync(Guid reportId)
    {
        var result = await dbContext.Checks
            .Include(e => e.Report)
            .Where(e => e.ReportId == reportId && e.Report.DeletedAt == null)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync();
        return result is null ? null : FromEntity(result);
    }

    public async Task<IEnumerable<Check>> GetAllChecksOfReportAsync(Guid reportId)
    {
        var result = await dbContext.Checks
            .Where(e => e.ReportId == reportId)
            .ToListAsync();
        return result.Select(FromEntity);
    }

    public async Task SetCheckStatusAsync(Guid checkId, ProgressStatus status)
    {
        await dbContext.Checks
            .Where(e => e.CheckId == checkId)
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.Status, status));
        await dbContext.SaveChangesAsync();
    }

    private static Check FromEntity(CheckEntity entity)
    {
        return new Check
        {
            Id = entity.CheckId,
            ReportId = entity.ReportId,
            UserId = entity.UserId,
            Name = entity.Name,
            Source = entity.Source,
            CreatedAt = entity.CreatedAt,
            Status = entity.Status,
        };
    }
}