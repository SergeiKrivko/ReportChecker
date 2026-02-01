using System.ComponentModel.DataAnnotations;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Entities;

public class IssueEntity
{
    public required Guid IssueId { get; init; }
    public required Guid CheckId { get; init; }
    [MaxLength(500)] public required string Title { get; init; }
    public int Priority { get; init; } = 1;
    [MaxLength(100)] public required string Chapter { get; init; }

    public virtual CheckEntity Check { get; init; } = null!;
    public virtual IEnumerable<CommentEntity> Comments { get; init; } = null!;
}