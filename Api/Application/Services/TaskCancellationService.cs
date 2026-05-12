using Microsoft.Extensions.Logging;
using ReportChecker.Abstractions;

namespace ReportChecker.Application.Services;

public class TaskCancellationService(ILogger<TaskCancellationService> logger) : ITaskCancellationService
{
    private readonly Dictionary<Guid, CancellationTokenSource> _checkCancellationTokens = [];
    private readonly Dictionary<Guid, CancellationTokenSource> _instructionCancellationTokens = [];

    public bool AddCheckCancellationToken(Guid checkId, CancellationTokenSource cancellationToken)
    {
        return _checkCancellationTokens.TryAdd(checkId, cancellationToken);
    }

    public bool DeleteCheckCancellationToken(Guid checkId)
    {
        return _checkCancellationTokens.Remove(checkId);
    }

    public async Task<bool> CancelCheckAsync(Guid checkId)
    {
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Cancelling check '{checkId}'", checkId);
        if (!_checkCancellationTokens.TryGetValue(checkId, out var token))
            return false;
        DeleteCheckCancellationToken(checkId);
        await token.CancelAsync();
        return true;
    }

    public bool AddInstructionCancellationToken(Guid taskId, CancellationTokenSource cancellationToken)
    {
        return _instructionCancellationTokens.TryAdd(taskId, cancellationToken);
    }

    public bool DeleteInstructionCancellationToken(Guid taskId)
    {
        return _instructionCancellationTokens.Remove(taskId);
    }

    public async Task<bool> CancelInstructionAsync(Guid taskId)
    {
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Cancelling instruction task '{taskId}'", taskId);
        if (!_instructionCancellationTokens.TryGetValue(taskId, out var token))
            return false;
        DeleteInstructionCancellationToken(taskId);
        await token.CancelAsync();
        return true;
    }
}