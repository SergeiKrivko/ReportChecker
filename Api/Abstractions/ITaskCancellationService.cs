namespace ReportChecker.Abstractions;

public interface ITaskCancellationService
{
    public bool AddCheckCancellationToken(Guid checkId, CancellationTokenSource cancellationToken);
    public bool DeleteCheckCancellationToken(Guid checkId);
    public Task<bool> CancelCheckAsync(Guid checkId);
    public bool AddInstructionCancellationToken(Guid checkId, CancellationTokenSource cancellationToken);
    public bool DeleteInstructionCancellationToken(Guid checkId);
    public Task<bool> CancelInstructionAsync(Guid checkId);
}