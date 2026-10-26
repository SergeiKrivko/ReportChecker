using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Converters;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Repositories;

public class IssueRepository(ReportCheckerDbContext dbContext) : IIssueRepository
{
    public async Task<IEnumerable<Issue>> GetAllIssuesOfCheckAsync(Guid checkId, CancellationToken ct = default)
    {
        var result = await dbContext.Issues
            .Where(e => e.CheckId == checkId)
            .Include(e => e.Comments)
            .ThenInclude(e => e.Patch).ThenInclude(e => e.Lines)
            .Include(e => e.Locations)
            .ToListAsync(ct);
        return result.Select(e => e.ToDomain());
    }

    public async Task<IEnumerable<Issue>> GetAllIssuesOfReportAsync(Guid reportId, CancellationToken ct = default)
    {
        var result = await dbContext.Issues
            .Include(e => e.Check)
            .Where(e => e.Check.ReportId == reportId)
            .Include(e => e.Comments)
            .ThenInclude(e => e.Patch).ThenInclude(e => e.Lines)
            .Include(e => e.Locations)
            .ToListAsync(ct);
        return result.Select(e => e.ToDomain());
    }

    public async Task<IEnumerable<Issue>> GetAllIssuesOfReportAsync(Guid reportId, Guid userId,
        CancellationToken ct = default)
    {
        var result = await dbContext.Issues
            .Include(e => e.Check)
            .Where(e => e.Check.ReportId == reportId)
            .Include(e => e.Comments).ThenInclude(e => e.Reads)
            .Include(e => e.Comments)
            .ThenInclude(e => e.Patch).ThenInclude(e => e.Lines)
            .Include(e => e.Locations)
            .ToListAsync(ct);
        return result.Select(e => e.ToDomain(userId));
    }

    public async Task<Issue?> GetIssueByIdAsync(Guid issueId, CancellationToken ct = default)
    {
        var result = await dbContext.Issues
            .Where(e => e.IssueId == issueId)
            .Include(e => e.Comments)
            .ThenInclude(e => e.Patch).ThenInclude(e => e.Lines)
            .Include(e => e.Locations)
            .FirstOrDefaultAsync(ct);
        return result?.ToDomain();
    }

    public async Task<Issue?> GetIssueByIdAsync(Guid issueId, Guid userId, CancellationToken ct = default)
    {
        var result = await dbContext.Issues
            .Where(e => e.IssueId == issueId)
            .Include(e => e.Comments).ThenInclude(e => e.Reads)
            .Include(e => e.Comments)
            .ThenInclude(e => e.Patch).ThenInclude(e => e.Lines)
            .Include(e => e.Locations)
            .FirstOrDefaultAsync(ct);
        return result?.ToDomain(userId);
    }

    public async Task<Guid> CreateIssueAsync(Guid checkId, string chapter, int line, string title, int? priority,
        CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        var entity = new IssueEntity
        {
            IssueId = id,
            CheckId = checkId,
            Title = title,
            Priority = priority ?? 1,
        };
        await dbContext.Issues.AddAsync(entity, ct);
        await dbContext.IssueLocations.AddAsync(new IssueLocationEntity
        {
            Id = Guid.NewGuid(),
            IssueId = id,
            CheckId = checkId,
            CreatedAt = DateTime.UtcNow,
            Chapter = chapter,
            Line = line,
        }, ct);
        await dbContext.SaveChangesAsync(ct);
        return id;
    }

    public async Task UpdateIssueLocationAsync(Guid issueId, Guid checkId, string chapter, int? line,
        CancellationToken ct = default)
    {
        await dbContext.IssueLocations.AddAsync(new IssueLocationEntity
        {
            Id = Guid.NewGuid(),
            IssueId = issueId,
            CheckId = checkId,
            CreatedAt = DateTime.UtcNow,
            Chapter = chapter,
            Line = line,
        }, ct);
        await dbContext.SaveChangesAsync(ct);
    }
}