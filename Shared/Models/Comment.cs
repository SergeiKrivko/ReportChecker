namespace ReportChecker.Shared.Models;

public class Comment
{
    public Guid Id { get; set; }
    public Guid IssueId { get; set; }
    public Guid UserId { get; set; }
    public string? Content { get; set; }
    public IssueStatus? Status { get; set; }
    public ProgressStatus? ProgressStatus { get; set; }
    public bool? IsRead { get; set; }
    public Patch? Patch { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}