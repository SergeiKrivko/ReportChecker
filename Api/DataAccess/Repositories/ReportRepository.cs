using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Converters;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Repositories;

public class ReportRepository(ReportCheckerDbContext dbContext) : IReportRepository
{
    public async Task<Guid> CreateReportAsync(Guid ownerId, string name, string format, string sourceProvider,
        Guid? llmModelId, ImageProcessingMode imageProcessingMode,
        LlmReasoningEffort reasoningLevel = LlmReasoningEffort.Default)
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
            ImageProcessingMode = imageProcessingMode,
            ReasoningLevel = reasoningLevel,
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

    public async Task<bool> UpdateReportAsync(Guid reportId, string newName, Guid? llmModelId,
        ImageProcessingMode imageProcessingMode, LlmReasoningEffort reasoningLevel = LlmReasoningEffort.Default)
    {
        var result = await dbContext.Reports
            .Where(e => e.ReportId == reportId && e.DeletedAt == null)
            .ExecuteUpdateAsync(e => e
                .SetProperty(x => x.Name, newName)
                .SetProperty(x => x.LlmModelId, llmModelId)
                .SetProperty(x => x.ImageProcessingMode, imageProcessingMode)
                .SetProperty(x => x.ReasoningLevel, reasoningLevel));
        await dbContext.SaveChangesAsync();
        return result > 0;
    }

    public async Task<Report?> GetReportByIdAsync(Guid reportId)
    {
        var result = await dbContext.Reports
            .Where(e => e.ReportId == reportId && e.DeletedAt == null)
            .FirstOrDefaultAsync();
        return result?.ToDomain();
    }

    public async Task<IEnumerable<Report>> GetAllReportsOfUserAsync(Guid userId)
    {
        var result = await dbContext.Reports
            .Where(e => e.OwnerId == userId && e.DeletedAt == null)
            .Include(e => e.FileSource)
            .Include(e => e.GitHubSource)
            .Include(e => e.LocalSource)
            .Include(e => e.Checks).ThenInclude(e => e.Issues).ThenInclude(e => e.Comments)
            .ToListAsync();
        return result.Select(e => e.ToDomain()).OrderByDescending(e => e.UpdatedAt);
    }

    public async Task<IEnumerable<Report>> GetAllReportsOfSourceAsync(string sourceProvider)
    {
        var result = await dbContext.Reports
            .Where(e => e.SourceProvider == sourceProvider && e.DeletedAt == null)
            .ToListAsync();
        return result.Select(e => e.ToDomain());
    }

    public async Task<int> CountReportsAsync(Guid userId)
    {
        return await dbContext.Reports
            .Where(e => e.OwnerId == userId && e.DeletedAt == null)
            .CountAsync();
    }
}