using ReportChecker.Shared.ApiClient;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Models;
using ReportChecker.Studio.Services.Converters;
using SubscriptionPlan = ReportChecker.Studio.Models.SubscriptionPlan;

namespace ReportChecker.Studio.Services;

public class SubscriptionService(IApiClient apiClient) : ISubscriptionService
{
    public async Task<CurrentSubscription> GetCurrentSubscriptionAsync(CancellationToken ct = default)
    {
        var resp = await apiClient.CurrentAsync(false, ct);
        return resp.ToDomain();
    }

    public async Task<SubscriptionPlan> GetSubscriptionPlanAsync(Guid? id, CancellationToken ct = default)
    {
        var resp = await apiClient.PlansAllAsync(ct);
        return resp.FirstOrDefault(e => e.Id == id)?.ToDomain() ?? new SubscriptionPlan
        {
            Name = "Free",
        };
    }
}