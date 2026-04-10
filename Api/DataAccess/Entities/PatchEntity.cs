using ReportChecker.Models;

namespace ReportChecker.DataAccess.Entities;

public class PatchEntity
{
    public required Guid Id { get; init; }
    public required Guid CommentId { get; init; }
    public required PatchStatus Status { get; init; }
    public required DateTime CreatedAt { get; init; }

    public CommentEntity Comment { get; init; } = null!;
    public ICollection<PatchLineEntity> Lines { get; init; } = [];
}