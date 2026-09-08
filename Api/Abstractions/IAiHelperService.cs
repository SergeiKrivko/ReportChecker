using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IAiHelperService
{
    public Task<IReadOnlyList<LLmModelPrice>> GetModelsPricingAsync(CancellationToken ct = default);
}