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

    public async Task<Report?> GetReportByIdAsync(Guid reportId, CancellationToken ct = default)
    {
        var result = await dbContext.Reports
            .Where(e => e.ReportId == reportId && e.DeletedAt == null)
            .FirstOrDefaultAsync(ct);
        return result?.ToDomain();
    }

    public async Task<ReportAccess?> GetReportAccessAsync(Guid reportId, Guid userId,
        CancellationToken ct = default)
    {
        var report = await dbContext.Reports
            .AsNoTracking()
            .Where(e => e.ReportId == reportId && e.DeletedAt == null)
            .FirstOrDefaultAsync(ct);
        if (report == null)
            return null;

        if (report.OwnerId == userId)
            return new ReportAccess
            {
                Report = report.ToDomain(),
                Level = ReportAccessLevel.Owner,
            };

        var level = await dbContext.ReportShares
            .AsNoTracking()
            .Where(e => e.ReportId == reportId && e.UserId == userId)
            .Select(e => (ReportAccessLevel?)e.AccessLevel)
            .FirstOrDefaultAsync(ct);
        if (level == null)
            return null;

        return new ReportAccess
        {
            Report = report.ToDomain(),
            Level = level.Value,
        };
    }

    public async Task<Report?> GetReportByShareTokenAsync(string token, CancellationToken ct = default)
    {
        var reportId = await dbContext.ReportShareLinks
            .AsNoTracking()
            .Where(e => e.Token == token)
            .Select(e => (Guid?)e.ReportId)
            .FirstOrDefaultAsync(ct);
        if (reportId == null)
            return null;

        return await GetReportByIdAsync(reportId.Value, ct);
    }

    public async Task<Report?> GetReportDetailsAsync(Guid reportId, CancellationToken ct = default)
    {
        var result = await ReportsWithDetails()
            .Where(e => e.ReportId == reportId && e.DeletedAt == null)
            .FirstOrDefaultAsync(ct);
        return result?.ToDomain();
    }

    public async Task<IEnumerable<ReportAccess>> GetAllReportsOfUserAsync(Guid userId,
        CancellationToken ct = default)
    {
        var reports = await ReportsWithDetails()
            .Where(e => e.DeletedAt == null &&
                        (e.OwnerId == userId || e.Shares.Any(s => s.UserId == userId)))
            .ToListAsync(ct);
        if (reports.Count == 0)
            return [];

        var reportIds = reports.Select(e => e.ReportId).ToArray();
        var levels = await dbContext.ReportShares
            .AsNoTracking()
            .Where(e => e.UserId == userId && reportIds.Contains(e.ReportId))
            .ToDictionaryAsync(e => e.ReportId, e => e.AccessLevel, ct);

        return reports
            .Where(e => e.OwnerId == userId || levels.ContainsKey(e.ReportId))
            .Select(e => new ReportAccess
            {
                Report = e.ToDomain(),
                Level = e.OwnerId == userId ? ReportAccessLevel.Owner : levels[e.ReportId],
            })
            .OrderByDescending(e => e.Report.UpdatedAt);
    }

    private IQueryable<ReportEntity> ReportsWithDetails()
    {
        return dbContext.Reports
            .AsNoTracking()
            .Include(e => e.FileSource)
            .Include(e => e.GitHubSource)
            .Include(e => e.LocalSource)
            .Include(e => e.Checks).ThenInclude(e => e.Issues).ThenInclude(e => e.Comments);
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