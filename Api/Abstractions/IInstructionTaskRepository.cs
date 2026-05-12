using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IInstructionTaskRepository
{
    public Task<IReadOnlyList<InstructionTask>> GetAllForReportAsync(Guid reportId, CancellationToken ct = default);

    public Task<IReadOnlyList<InstructionTask>> GetAllForReportAsync(Guid reportId, ProgressStatus status,
        CancellationToken ct = default);

    public Task<InstructionTask?> GetByIdAsync(Guid taskId, CancellationToken ct = default);

    public Task<Guid> CreateAsync(Guid reportId, string instruction, InstructionTaskMode mode,
        ProgressStatus status = ProgressStatus.Queued, CancellationToken ct = default);

    public Task<bool> SetStatusAsync(Guid taskId, ProgressStatus status, CancellationToken ct = default);
}