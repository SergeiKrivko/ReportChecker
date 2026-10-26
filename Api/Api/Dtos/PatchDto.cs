using ReportChecker.Models;

namespace ReportChecker.Api.Dtos;

public class PatchDto
{
    public required Guid Id { get; init; }
    public required Guid CommentId { get; init; }
    public required PatchStatus Status { get; init; }
    public IReadOnlyList<PatchLineDto> Lines { get; init; } = [];
    public required DateTime CreatedAt { get; init; }
}