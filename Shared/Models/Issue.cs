namespace ReportChecker.Shared.Models;

public class Issue
{
    public Guid Id { get; set; }
    public Guid CheckId { get; set; }
    public string? Title { get; set; }
    public IssueStatus? Status { get; set; }
    public int? Line { get; set; }
    public int? Priority { get; set; }
    public string? Chapter { get; set; }
    public IReadOnlyList<Comment> Comments { get; set; } = [];
    public bool IsRead { get; init; }
}