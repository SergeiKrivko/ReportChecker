using System.ComponentModel.DataAnnotations;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Entities;

public class PatchLineEntity
{
    public required Guid Id { get; init; }
    public required Guid PatchId { get; init; }
    public required int Number { get; init; }
    public required int Index { get; init; }
    [MaxLength(2048)] public string? Content { get; init; }
    public PatchLineType Type { get; init; }
    public required DateTime CreatedAt { get; init; }

    public PatchEntity Patch { get; init; } = null!;
}