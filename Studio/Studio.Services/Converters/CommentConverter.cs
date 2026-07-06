using ReportChecker.Shared.Models;

namespace ReportChecker.Studio.Services.Converters;

public static class CommentConverter
{
    public static Comment ToDomain(this Shared.ApiClient.Comment dto)
    {
        return new Comment
        {
            Id = dto.Id,
            IssueId = dto.IssueId,
            IsRead = dto.IsRead,
            UserId = dto.UserId,
            Content = dto.Content,
            CreatedAt = dto.CreatedAt,
            DeletedAt = dto.DeletedAt,
            ModifiedAt = dto.ModifiedAt,
            ProgressStatus = dto.ProgressStatus?.ToDomain(),
            Status = dto.Status?.ToDomain()
        };
    }
}