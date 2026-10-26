using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Converters;

public static class CommentConverter
{
    public static Comment ToDomain(this CommentEntity entity, Guid? userId = null)
    {
        return new Comment
        {
            Id = entity.CommentId,
            IssueId = entity.IssueId,
            UserId = entity.UserId,
            Content = entity.Content,
            Status = entity.Status,
            ProgressStatus = entity.ProgressStatus,
            IsRead = userId == null ? null : entity.Reads.FirstOrDefault(e => e.UserId == userId) != null,
            Patch = entity.Patch?.ToDomain(),
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt,
            DeletedAt = entity.DeletedAt,
        };
    }
}