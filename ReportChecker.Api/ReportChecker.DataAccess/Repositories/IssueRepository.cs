using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Repositories;

public class IssueRepository(ReportCheckerDbContext dbContext) : IIssueRepository
{
    public async Task<IEnumerable<Issue>> GetAllIssuesOfCheckAsync(Guid checkId)
    {
        var result = await dbContext.Issues
            .Where(e => e.CheckId == checkId)
            .Include(e => e.Comments)
            .ThenInclude(e => e.Patch).ThenInclude(e => e.Lines)
            .ToListAsync();
        return result.Select(e => FromEntity(e));
    }

    public async Task<IEnumerable<Issue>> GetAllIssuesOfReportAsync(Guid reportId)
    {
        var result = await dbContext.Issues
            .Include(e => e.Check)
            .Where(e => e.Check.ReportId == reportId)
            .Include(e => e.Comments)
            .ThenInclude(e => e.Patch).ThenInclude(e => e.Lines)
            .ToListAsync();
        return result.Select(e => FromEntity(e));
    }

    public async Task<IEnumerable<Issue>> GetAllIssuesOfReportAsync(Guid reportId, Guid userId)
    {
        var result = await dbContext.Issues
            .Include(e => e.Check)
            .Where(e => e.Check.ReportId == reportId)
            .Include(e => e.Comments).ThenInclude(e => e.Reads)
            .Include(e => e.Comments)
            .ThenInclude(e => e.Patch).ThenInclude(e => e.Lines)
            .ToListAsync();
        return result.Select(e => FromEntity(e, userId));
    }

    public async Task<Issue?> GetIssueByIdAsync(Guid issueId)
    {
        var result = await dbContext.Issues
            .Where(e => e.IssueId == issueId)
            .Include(e => e.Comments)
            .ThenInclude(e => e.Patch).ThenInclude(e => e.Lines)
            .FirstOrDefaultAsync();
        return result is null ? null : FromEntity(result);
    }

    public async Task<Issue?> GetIssueByIdAsync(Guid issueId, Guid userId)
    {
        var result = await dbContext.Issues
            .Where(e => e.IssueId == issueId)
            .Include(e => e.Comments).ThenInclude(e => e.Reads)
            .Include(e => e.Comments)
            .ThenInclude(e => e.Patch).ThenInclude(e => e.Lines)
            .FirstOrDefaultAsync();
        return result is null ? null : FromEntity(result, userId);
    }

    public async Task<Guid> CreateIssueAsync(Guid checkId, string chapter, string title, int? priority)
    {
        var id = Guid.NewGuid();
        var entity = new IssueEntity
        {
            IssueId = id,
            CheckId = checkId,
            Title = title,
            Priority = priority ?? 1,
            Chapter = chapter,
        };
        await dbContext.Issues.AddAsync(entity);
        await dbContext.SaveChangesAsync();
        return id;
    }

    private static Issue FromEntity(IssueEntity entity, Guid? userId = null)
    {
        return new Issue
        {
            Id = entity.IssueId,
            CheckId = entity.CheckId,
            Title = entity.Title,
            Priority = entity.Priority,
            Status = entity.Comments
                .Where(e => e.Status != null)
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefault()
                ?.Status ?? IssueStatus.Open,
            Comments = entity.Comments
                .Where(e => e.DeletedAt == null)
                .OrderBy(e => e.CreatedAt)
                .Select(x => CommentRepository.FromEntity(x, userId))
                .ToArray(),
            Chapter = entity.Chapter,
        };
    }
}