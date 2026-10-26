using ReportChecker.Models;

namespace ReportChecker.Api.Dtos;

public class CommentDto
{
    public Guid Id { get; init; }
    public Guid IssueId { get; init; }
    public Guid UserId { get; init; }
    public string? Content { get; init; }
    public IssueStatus? Status { get; init; }
    public ProgressStatus? ProgressStatus { get; init; }
    public bool? IsRead { get; init; }
    public PatchDto? Patch { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
}