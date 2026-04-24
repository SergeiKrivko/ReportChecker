using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface ISubscriptionService
{
    public Task<UserSubscription?> GetActiveSubscription(Guid userId, CancellationToken ct = default);
    public Task<Limit<int>> GetReportsLimitAsync(Guid userId, CancellationToken ct = default);
    public Task<Limit<int>> GetTokensLimitAsync(Guid userId, CancellationToken ct = default);
    public Task<bool> CheckTokensLimitAsync(Guid userId, CancellationToken ct = default);
    public Task<bool> CheckReportsLimitAsync(Guid userId, CancellationToken ct = default);
    public Task<CreatedSubscription> CreateSubscriptionAsync(Guid userId, Guid offerId, CancellationToken ct = default);
    public Task ConfirmSubscriptionAsync(Guid subscriptionId, CancellationToken ct = default);
}