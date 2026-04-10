using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IInstructionTaskService
{
    public Task<IReadOnlyList<InstructionTask>> GetActiveInstructionTasksAsync(Guid reportId,
        CancellationToken ct = default);

    public Task<Guid> CreateInstructionTaskAsync(Guid reportId, Guid instructionId, InstructionTaskMode mode,
        CancellationToken ct = default);

    public Task<Guid> CreateInstructionTaskAsync(Guid reportId, string instruction, InstructionTaskMode mode,
        CancellationToken ct = default);
}