namespace ReportChecker.Models;

public class Patch
{
    public required Guid Id { get; init; }
    public required Guid CommentId { get; init; }
    public required PatchStatus Status { get; init; }
    public IReadOnlyList<PatchLine> Lines { get; init; } = [];
    public required DateTime CreatedAt { get; init; }
}
