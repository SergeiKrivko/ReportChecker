using System.ComponentModel.DataAnnotations;

namespace ReportChecker.DataAccess.Entities;

public class IssueLocationEntity
{
    public required Guid Id { get; init; }
    public required Guid IssueId { get; init; }
    public required Guid CheckId { get; init; }
    public required DateTime CreatedAt { get; init; }
    [MaxLength(256)] public required string Chapter { get; init; }
    public int? Line { get; init; }

    public virtual CheckEntity Check { get; init; } = null!;
    public virtual IssueEntity Issue { get; init; } = null!;
}