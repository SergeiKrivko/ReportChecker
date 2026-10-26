using ReportChecker.Models;

namespace ReportChecker.Api.Dtos;

public class InstructionTaskDto
{
    public required Guid Id { get; init; }
    public required Guid ReportId { get; init; }
    public ProgressStatus Status { get; init; }
    public InstructionTaskMode Mode { get; init; }
    public required string Instruction { get; init; }
}