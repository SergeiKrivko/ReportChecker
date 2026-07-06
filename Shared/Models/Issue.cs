namespace ReportChecker.Shared.Models;

public class Issue
{
    public System.Guid Id { get; set; }
    public System.Guid CheckId { get; set; }
    public string? Title { get; set; }
    public IssueStatus? Status { get; set; }
    public int? Priority { get; set; }
    public string? Chapter { get; set; }
    public IReadOnlyList<Comment> Comments { get; set; } = [];
}