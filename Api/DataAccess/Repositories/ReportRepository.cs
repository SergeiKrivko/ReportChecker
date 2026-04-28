using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Repositories;

public class ReportRepository(ReportCheckerDbContext dbContext) : IReportRepository
{
    public async Task<Guid> CreateReportAsync(Guid ownerId, string name, string format, string sourceProvider,
        Guid? llmModelId, ImageProcessingMode imageProcessingMode)
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
        ImageProcessingMode imageProcessingMode)
    {
        var result = await dbContext.Reports
            .Where(e => e.ReportId == reportId && e.DeletedAt == null)
            .ExecuteUpdateAsync(e => e
                .SetProperty(x => x.Name, newName)
                .SetProperty(x => x.LlmModelId, llmModelId)
                .SetProperty(x => x.ImageProcessingMode, imageProcessingMode));
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
            .Include(e => e.FileSource)
            .Include(e => e.GitHubSource)
            .Include(e => e.LocalSource)
            .Include(e => e.Checks).ThenInclude(e => e.Issues).ThenInclude(e => e.Comments)
            .ToListAsync();
        return result.Select(FromEntity).OrderByDescending(e => e.UpdatedAt);
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
        DateTime updatedAt;
        try
        {
            updatedAt = entity.Checks.SelectMany(e =>
                    e.Issues.SelectMany(i => i.Comments.Select(c => c.CreatedAt)))
                .Max();
        }
        catch (InvalidOperationException)
        {
            updatedAt = entity.CreatedAt;
        }

        return new Report
        {
            Id = entity.ReportId,
            OwnerId = entity.OwnerId,
            Name = entity.Name,
            SourceProvider = entity.SourceProvider,
            Format = entity.Format,
            LlmModelId = entity.LlmModelId,
            ImageProcessingMode = entity.ImageProcessingMode,
            CreatedAt = entity.CreatedAt,
            DeletedAt = entity.DeletedAt,

            IssueCount = entity.Checks
                .SelectMany(e => e.Issues)
                .GroupBy(e => e.Priority)
                .ToDictionary(e => e.Key,
                    e => e.Count(i => i.Comments
                        .Where(c => c.Status != null)
                        .OrderByDescending(c => c.CreatedAt)
                        .First().Status == IssueStatus.Open)),
            UpdatedAt = updatedAt,
            Source = new ReportSourceUnion
            {
                File = entity.FileSource == null
                    ? null
                    : new FileReportSource
                    {
                        InitialFileId = entity.FileSource.InitialFileId,
                        EntryFilePath = entity.FileSource.EntryFilePath,
                    },
                GitHub = entity.GitHubSource == null
                    ? null
                    : new GitHubReportSource()
                    {
                        RepositoryId = entity.GitHubSource.RepositoryId,
                        Branch = entity.GitHubSource.Branch,
                        Path = entity.GitHubSource.Path,
                    },
                Local = entity.LocalSource == null
                    ? null
                    : new LocalReportSource()
                    {
                        InitialFileId = entity.LocalSource.InitialFileId,
                        EntryFilePath = entity.LocalSource.EntryFilePath,
                        ClientId = entity.LocalSource.ClientId,
                        ClientMachineName = entity.LocalSource.ClientMachineName
                    },
            }
        };
    }
}