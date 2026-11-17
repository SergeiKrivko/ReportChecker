using System.ComponentModel.DataAnnotations;

namespace ReportChecker.DataAccess.Entities;

public class ReportShareLinkEntity
{
    public required Guid ReportId { get; init; }
    [MaxLength(64)] public required string Token { get; init; }
    public DateTime CreatedAt { get; init; }

    public virtual ReportEntity Report { get; init; } = null!;
}
