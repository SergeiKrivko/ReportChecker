using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.Abstractions;

public interface ISubscriptionService
{
    public Task<CurrentSubscription> GetCurrentSubscriptionAsync(CancellationToken ct = default);
    public Task<SubscriptionPlan> GetSubscriptionPlanAsync(Guid? id, CancellationToken ct = default);
}