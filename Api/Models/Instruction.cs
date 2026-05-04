namespace ReportChecker.Models;

public class Instruction
{
    public Guid Id { get; init; }
    public Guid ReportId { get; init; }
    public Guid UserId { get; init; }
    public Guid? CommentId { get; init; }
    public required string Content { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
}