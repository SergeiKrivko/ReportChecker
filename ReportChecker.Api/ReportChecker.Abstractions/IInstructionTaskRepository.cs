using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IInstructionTaskRepository
{
    public Task<IEnumerable<InstructionTask>> GetAllForReportAsync(Guid reportId);
    public Task<IEnumerable<InstructionTask>> GetAllForReportAsync(Guid reportId, ProgressStatus status);
    public Task<Guid> CreateAsync(Guid reportId, string instruction, ProgressStatus status = ProgressStatus.Queued);
    public Task<bool> SetStatusAsync(Guid taskId, ProgressStatus status);
}