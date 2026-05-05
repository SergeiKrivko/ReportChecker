using ReportChecker.Abstractions;

namespace ReportChecker.Application.Services;

public class TaskCancellationService : ITaskCancellationService
{
    private readonly Dictionary<Guid, CancellationTokenSource> _checkCancellationTokens = [];

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
        if (!_checkCancellationTokens.TryGetValue(checkId, out var token))
            return false;
        await token.CancelAsync();
        return true;
    }
}