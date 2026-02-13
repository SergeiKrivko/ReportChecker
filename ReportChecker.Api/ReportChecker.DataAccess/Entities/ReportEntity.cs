using System.ComponentModel.DataAnnotations;

namespace ReportChecker.DataAccess.Entities;

public class ReportEntity
{
    public required Guid ReportId { get; init; }
    public required Guid OwnerId { get; init; }
    [MaxLength(100)] public string? Name { get; init; }
    [MaxLength(20)] public required string SourceProvider { get; init; }
    [MaxLength(20)] public required string Format { get; init; }
    [MaxLength(200)] public string? Source { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }

    public virtual IEnumerable<CheckEntity> Checks { get; init; } = null!;
}