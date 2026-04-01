namespace ReportChecker.Models;

public class CommentReading
{
    public required Guid CommentId { get; init; }
    public required Guid UserId { get; init; }
    public required DateTime CreatedAt { get; init; }
}