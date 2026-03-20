namespace ReportChecker.Models;

public class InstructionTask
{
    public required Guid Id { get; init; }
    public required Guid ReportId { get; init; }
    public ProgressStatus Status { get; init; }
    public required string Instruction { get; init; }
}