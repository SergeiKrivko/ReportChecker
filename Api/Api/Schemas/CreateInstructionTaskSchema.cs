using ReportChecker.Models;

namespace ReportChecker.Api.Schemas;

public class CreateInstructionTaskSchema
{
    public Guid? InstructionId { get; init; }
    public string? Instruction { get; init; }
    public InstructionTaskMode Mode { get; init; }
}