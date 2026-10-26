using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Converters;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Repositories;

public class CommentRepository(ReportCheckerDbContext dbContext) : ICommentRepository
{
    public async Task<IEnumerable<Comment>> GetAllCommentsOfIssueAsync(Guid issueId)
    {
        var result = await dbContext.Comments
            .Where(e => e.IssueId == issueId && e.DeletedAt == null)
            .Include(e => e.Patch).ThenInclude(e => e.Lines)
            .ToListAsync();
        return result.Select(e => e.ToDomain());
    }

    public async Task<IReadOnlyList<Comment>> GetAllCommentsOfIssueAsync(Guid issueId, Guid userId)
    {
        var result = await dbContext.Comments
            .Where(e => e.IssueId == issueId && e.DeletedAt == null)
            .Include(e => e.Reads)
            .Include(e => e.Patch).ThenInclude(e => e.Lines)
            .ToListAsync();
        return result.Select(e => e.ToDomain(userId)).ToList();
    }

    public async Task<Comment?> GetCommentByIdAsync(Guid commentId)
    {
        var result = await dbContext.Comments
            .Where(e => e.CommentId == commentId && e.DeletedAt == null)
            .Include(e => e.Patch).ThenInclude(e => e.Lines)
            .FirstOrDefaultAsync();
        return result?.ToDomain();
    }

    public async Task<Comment?> GetCommentByIdAsync(Guid commentId, Guid userId)
    {
        var result = await dbContext.Comments
            .Where(e => e.CommentId == commentId && e.DeletedAt == null)
            .Include(e => e.Reads)
            .Include(e => e.Patch).ThenInclude(e => e.Lines)
            .FirstOrDefaultAsync();
        return result?.ToDomain(userId);
    }

    public async Task<Guid> CreateCommentAsync(Guid issueId, Guid userId, string? content, IssueStatus? status,
        ProgressStatus? progressStatus = null)
    {
        var id = Guid.NewGuid();
        var entity = new CommentEntity
        {
            CommentId = id,
            IssueId = issueId,
            UserId = userId,
            Content = content,
            Status = status,
            ProgressStatus = progressStatus,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = null,
            DeletedAt = null,
        };
        await dbContext.Comments.AddAsync(entity);
        await dbContext.SaveChangesAsync();
        return id;
    }

    public async Task UpdateCommentAsync(Guid commentId, string comment)
    {
        await dbContext.Comments
            .Where(e => e.CommentId == commentId && e.DeletedAt == null)
            .ExecuteUpdateAsync(e =>
            {
                e.SetProperty(x => x.ModifiedAt, DateTime.UtcNow);
                e.SetProperty(x => x.Content, comment);
            });
        await dbContext.SaveChangesAsync();
    }

    public async Task FinishCommentAsync(Guid commentId, string? comment, IssueStatus? status)
    {
        await dbContext.Comments
            .Where(e => e.CommentId == commentId && e.DeletedAt == null)
            .ExecuteUpdateAsync(e =>
            {
                e.SetProperty(x => x.ProgressStatus, ProgressStatus.Completed);
                e.SetProperty(x => x.Status, status);
                e.SetProperty(x => x.Content, comment);
            });
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteCommentAsync(Guid commentId)
    {
        await dbContext.Comments
            .Where(e => e.CommentId == commentId && e.DeletedAt == null)
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.DeletedAt, DateTime.UtcNow));
        await dbContext.SaveChangesAsync();
    }

    public async Task SetProgressStatusAsync(Guid commentId, ProgressStatus status)
    {
        await dbContext.Comments
            .Where(e => e.CommentId == commentId)
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.ProgressStatus, status));
        await dbContext.SaveChangesAsync();
    }

    public async Task<int> CountAiCommentsAsync(Guid userId, DateTime startDate)
    {
        return await dbContext.Comments
            .Where(e => e.UserId == userId && e.ProgressStatus != null)
            .CountAsync();
    }
}