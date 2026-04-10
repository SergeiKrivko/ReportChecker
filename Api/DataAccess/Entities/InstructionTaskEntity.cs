using System.ComponentModel.DataAnnotations;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Entities;

public class InstructionTaskEntity
{
    public required Guid Id { get; init; }
    public required Guid ReportId { get; init; }
    public ProgressStatus Status { get; init; }
    public InstructionTaskMode Mode { get; init; }
    [MaxLength(1024)] public required string Instruction { get; init; }
    public DateTime CreatedAt { get; init; }

    public ReportEntity Report { get; init; } = null!;
}