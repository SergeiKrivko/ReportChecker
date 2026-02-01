namespace ReportChecker.Models;

public class Comment
{
    public Guid Id { get; init; }
    public Guid IssueId { get; init; }
    public Guid UserId { get; init; }
    public string? Content { get; init; }
    public IssueStatus? Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
}