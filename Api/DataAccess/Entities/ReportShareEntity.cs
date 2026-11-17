using ReportChecker.Models;

namespace ReportChecker.DataAccess.Entities;

public class ReportShareEntity
{
    public required Guid ReportId { get; init; }
    public required Guid UserId { get; init; }
    public required ReportAccessLevel AccessLevel { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    public virtual ReportEntity Report { get; init; } = null!;
}
