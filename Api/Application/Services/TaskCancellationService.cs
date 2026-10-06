using Microsoft.Extensions.Logging;
using ReportChecker.Abstractions;

namespace ReportChecker.Application.Services;

public class TaskCancellationService(ILogger<TaskCancellationService> logger) : ITaskCancellationService
{
    private readonly Dictionary<Guid, CancellationTokenSource> _checkCancellationTokens = [];
    private readonly Dictionary<Guid, CancellationTokenSource> _instructionCancellationTokens = [];
    private readonly Dictionary<Guid, CancellationTokenSource> _benchmarkCancellationTokens = [];

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

    public bool AddBenchmarkCancellationToken(Guid runId, CancellationTokenSource cancellationToken)
    {
        return _benchmarkCancellationTokens.TryAdd(runId, cancellationToken);
    }

    public bool DeleteBenchmarkCancellationToken(Guid runId)
    {
        return _benchmarkCancellationTokens.Remove(runId);
    }

    public async Task<bool> CancelBenchmarkAsync(Guid runId)
    {
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Cancelling benchmark run '{runId}'", runId);
        if (!_benchmarkCancellationTokens.TryGetValue(runId, out var token))
            return false;
        DeleteBenchmarkCancellationToken(runId);
        await token.CancelAsync();
        return true;
    }
}
