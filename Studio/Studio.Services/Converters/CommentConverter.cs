using ReportChecker.Shared.Models;

namespace ReportChecker.Studio.Services.Converters;

public static class CommentConverter
{
    public static Comment ToDomain(this Shared.ApiClient.Comment dto)
    {
        return new Comment
        {
            Id = dto.Id ?? throw new Exception(),
            IssueId = dto.IssueId ?? throw new Exception(),
            IsRead = dto.IsRead,
            UserId = dto.UserId ?? throw new Exception(),
            Content = dto.Content,
            CreatedAt = dto.CreatedAt ?? throw new Exception(),
            DeletedAt = dto.DeletedAt,
            ModifiedAt = dto.ModifiedAt,
            ProgressStatus = dto.ProgressStatus?.ToDomain(),
            Status = dto.Status?.ToDomain(),
            Patch = dto.Patch?.ToDomain(),
        };
    }
}