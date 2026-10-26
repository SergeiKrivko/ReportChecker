using ReportChecker.Models;

namespace ReportChecker.Api.Dtos;

public class CreateInstructionTaskDto
{
    public Guid? InstructionId { get; init; }
    public string? Instruction { get; init; }
    public InstructionTaskMode Mode { get; init; }
}