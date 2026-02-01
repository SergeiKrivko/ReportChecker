using System.ComponentModel.DataAnnotations;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Entities;

public class CheckEntity
{
    public required Guid CheckId { get; init; }
    public required Guid ReportId { get; init; }
    public required Guid UserId { get; init; }
    [MaxLength(100)] public string? Name { get; init; }
    public DateTime CreatedAt { get; init; }
    [MaxLength(200)] public string? Source { get; init; }
    public ProgressStatus Status { get; init; }

    public virtual UserEntity User { get; init; } = null!;
    public virtual ReportEntity Report { get; init; } = null!;
    public virtual IEnumerable<IssueEntity> Issues { get; init; } = null!;
}