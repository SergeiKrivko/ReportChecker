using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Repositories;

public class ReportRepository(ReportCheckerDbContext dbContext) : IReportRepository
{
    public async Task<Guid> CreateReportAsync(Guid ownerId, string name, string format, string sourceProvider,
        string? source)
    {
        var id = Guid.NewGuid();
        var entity = new ReportEntity
        {
            ReportId = id,
            OwnerId = ownerId,
            Name = name,
            SourceProvider = sourceProvider,
            Source = source,
            Format = format,
            CreatedAt = DateTime.UtcNow,
            DeletedAt = null,
        };
        await dbContext.Reports.AddAsync(entity);
        await dbContext.SaveChangesAsync();
        return id;
    }

    public async Task<bool> DeleteReportAsync(Guid reportId)
    {
        var result = await dbContext.Reports
            .Where(e => e.ReportId == reportId && e.DeletedAt == null)
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.DeletedAt, DateTime.UtcNow));
        await dbContext.SaveChangesAsync();
        return result > 0;
    }

    public async Task<bool> RenameReportAsync(Guid reportId, string newName)
    {
        var result = await dbContext.Reports
            .Where(e => e.ReportId == reportId && e.DeletedAt == null)
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.Name, newName));
        await dbContext.SaveChangesAsync();
        return result > 0;
    }

    public async Task<Report?> GetReportByIdAsync(Guid reportId)
    {
        var result = await dbContext.Reports
            .Where(e => e.ReportId == reportId && e.DeletedAt == null)
            .FirstOrDefaultAsync();
        return result is null ? null : FromEntity(result);
    }

    public async Task<IEnumerable<Report>> GetAllReportsOfUserAsync(Guid userId)
    {
        var result = await dbContext.Reports
            .Where(e => e.OwnerId == userId && e.DeletedAt == null)
            .ToListAsync();
        return result.Select(FromEntity);
    }

    public async Task<IEnumerable<Report>> GetAllReportsOfSourceAsync(string sourceProvider)
    {
        var result = await dbContext.Reports
            .Where(e => e.SourceProvider == sourceProvider && e.DeletedAt == null)
            .ToListAsync();
        return result.Select(FromEntity);
    }

    private static Report FromEntity(ReportEntity entity)
    {
        return new Report
        {
            Id = entity.ReportId,
            OwnerId = entity.OwnerId,
            Name = entity.Name,
            SourceProvider = entity.SourceProvider,
            Source = entity.SourceProvider,
            Format = entity.Format,
            CreatedAt = entity.CreatedAt,
            DeletedAt = entity.DeletedAt,
        };
    }
}