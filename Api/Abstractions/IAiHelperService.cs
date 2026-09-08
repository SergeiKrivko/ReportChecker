using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IAiHelperService
{
    public Task<IReadOnlyList<ModelPrice>> GetModelsPricingAsync(CancellationToken ct = default);
}