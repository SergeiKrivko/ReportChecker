using Microsoft.Extensions.Configuration;

namespace ReportChecker.Application.Services;

/// <summary>
/// Ограничивает число одновременно выполняемых прогонов бенчмарка.
/// Регистрируется как singleton, иначе лимит не действует при выполнении прогона в отдельном scope.
/// </summary>
public sealed class BenchmarkRunLimiter : IDisposable
{
    private readonly SemaphoreSlim _semaphore;

    public BenchmarkRunLimiter(IConfiguration configuration)
    {
        var limit = int.Parse(configuration["Benchmarks.MaxParallelRuns"] ?? "2");
        _semaphore = new SemaphoreSlim(Math.Max(1, limit));
    }

    public Task WaitAsync(CancellationToken ct = default)
    {
        return _semaphore.WaitAsync(ct);
    }

    public void Release()
    {
        _semaphore.Release();
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}
