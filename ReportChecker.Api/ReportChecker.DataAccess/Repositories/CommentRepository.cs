using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Repositories;

public class CommentRepository(ReportCheckerDbContext dbContext) : ICommentRepository
{
    public async Task<IEnumerable<Comment>> GetAllCommentsOfIssueAsync(Guid issueId)
    {
        var result = await dbContext.Comments
            .Where(e => e.IssueId == issueId && e.DeletedAt == null)
            .ToListAsync();
        return result.Select(FromEntity);
    }

    public async Task<Comment?> GetCommentByIdAsync(Guid commentId)
    {
        var result = await dbContext.Comments
            .Where(e => e.CommentId == commentId && e.DeletedAt == null)
            .FirstOrDefaultAsync();
        return result is null ? null : FromEntity(result);
    }

    public async Task<Guid> CreateCommentAsync(Guid issueId, Guid userId, string? content, IssueStatus? status)
    {
        var id = Guid.NewGuid();
        var entity = new CommentEntity
        {
            CommentId = id,
            IssueId = issueId,
            UserId = userId,
            Content = content,
            Status = status,
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

    internal static Comment FromEntity(CommentEntity entity)
    {
        return new Comment
        {
            Id = entity.CommentId,
            IssueId = entity.IssueId,
            UserId = entity.UserId,
            Content = entity.Content,
            Status = entity.Status,
            ProgressStatus = entity.ProgressStatus,
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt,
            DeletedAt = entity.DeletedAt,
        };
    }
}