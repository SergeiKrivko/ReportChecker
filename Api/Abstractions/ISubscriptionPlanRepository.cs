using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface ISubscriptionPlanRepository
{
    public Task<IReadOnlyList<SubscriptionPlan>> GetAllPlansAsync(CancellationToken ct = default);

    public Task<SubscriptionPlan?> GetPlanByIdAsync(Guid id, CancellationToken ct = default);

    public Task<Guid> CreatePlanAsync(string name, string? description, int tokenLimit, int reportsLimit, 
        bool isHidden, CancellationToken ct = default);

    public Task<bool> UpdatePlanAsync(Guid id, string name, string? description, int tokenLimit, int reportsLimit, 
        bool isHidden, CancellationToken ct = default);

    public Task<bool> DeletePlanAsync(Guid id, CancellationToken ct = default);
}