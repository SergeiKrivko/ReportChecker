using System.ComponentModel.DataAnnotations;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Entities;

public class CommentEntity
{
    public Guid CommentId { get; init; }
    public Guid IssueId { get; init; }
    public Guid UserId { get; init; }
    [MaxLength(8000)] public string? Content { get; init; }
    public IssueStatus? Status { get; init; }
    public ProgressStatus? ProgressStatus { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
    public DateTime? DeletedAt { get; init; }

    public IssueEntity Issue { get; init; } = null!;
}