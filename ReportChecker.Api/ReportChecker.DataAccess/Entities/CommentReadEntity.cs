namespace ReportChecker.DataAccess.Entities;

public class CommentReadEntity
{
    public required Guid CommentId { get; init; }
    public required Guid UserId { get; init; }
    public required DateTime CreatedAt { get; init; }

    public CommentEntity Comment { get; init; } = null!;
}