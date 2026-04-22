using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Repositories;

public class ReportRepository(ReportCheckerDbContext dbContext) : IReportRepository
{
    public async Task<Guid> CreateReportAsync(Guid ownerId, string name, string format, string sourceProvider,
        Guid? llmModelId)
    {
        var id = Guid.NewGuid();
        var entity = new ReportEntity
        {
            ReportId = id,
            OwnerId = ownerId,
            Name = name,
            SourceProvider = sourceProvider,
            Format = format,
            LlmModelId = llmModelId,
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

    public async Task<bool> RenameReportAsync(Guid reportId, string newName, Guid? llmModelId)
    {
        var result = await dbContext.Reports
            .Where(e => e.ReportId == reportId && e.DeletedAt == null)
            .ExecuteUpdateAsync(e => e
                .SetProperty(x => x.Name, newName)
                .SetProperty(x => x.LlmModelId, llmModelId));
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

    public async Task<int> CountReportsAsync(Guid userId)
    {
        return await dbContext.Reports
            .Where(e => e.OwnerId == userId && e.DeletedAt == null)
            .CountAsync();
    }

    private static Report FromEntity(ReportEntity entity)
    {
        return new Report
        {
            Id = entity.ReportId,
            OwnerId = entity.OwnerId,
            Name = entity.Name,
            SourceProvider = entity.SourceProvider,
            Format = entity.Format,
            LlmModelId = entity.LlmModelId,
            CreatedAt = entity.CreatedAt,
            DeletedAt = entity.DeletedAt,
        };
    }
}