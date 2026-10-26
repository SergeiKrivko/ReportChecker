using ReportChecker.Api.Dtos;
using ReportChecker.Models;

namespace ReportChecker.Api.Converters;

public static class CommentConverter
{
    public static CommentDto ToDto(this Comment comment)
    {
        return new CommentDto
        {
            Id = comment.Id,
            IssueId = comment.IssueId,
            UserId = comment.UserId,
            Content = comment.Content,
            Status = comment.Status,
            ProgressStatus = comment.ProgressStatus,
            IsRead = comment.IsRead,
            Patch = comment.Patch?.ToDto(),
            CreatedAt = comment.CreatedAt,
            ModifiedAt = comment.ModifiedAt,
            DeletedAt = comment.DeletedAt,
        };
    }
}