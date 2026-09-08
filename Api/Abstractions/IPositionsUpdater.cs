using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IPositionsUpdater
{
    public Task UpdatePositionsAsync(CheckContext context, CancellationToken ct = default);
}