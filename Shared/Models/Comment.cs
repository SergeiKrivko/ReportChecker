namespace ReportChecker.Shared.Models;

public class Comment
{
    public System.Guid? Id { get; set; }
    public System.Guid? IssueId { get; set; }
    public System.Guid? UserId { get; set; }
    public string? Content { get; set; }
    public IssueStatus? Status { get; set; }
    public ProgressStatus? ProgressStatus { get; set; }
    public bool? IsRead { get; set; }
    public Patch? Patch { get; set; }
    public System.DateTimeOffset? CreatedAt { get; set; }
    public System.DateTimeOffset? ModifiedAt { get; set; }
    public System.DateTimeOffset? DeletedAt { get; set; }
}