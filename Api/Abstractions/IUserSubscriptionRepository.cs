using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IUserSubscriptionRepository
{
    public Task<UserSubscription?> GetSubscriptionByIdAsync(Guid id, CancellationToken ct = default);
    public Task<UserSubscription?> GetActiveSubscriptionAsync(Guid userId, CancellationToken ct = default);
    public Task<UserSubscription?> GetLastSubscriptionAsync(Guid userId, CancellationToken ct = default);
    public Task<IReadOnlyList<UserSubscription>> GetFutureSubscriptionsAsync(Guid userId, CancellationToken ct = default);

    public Task<UserSubscription> CreateSubscriptionAsync(Guid planId, Guid userId, decimal defaultPrice, decimal price,
        DateTime startsAt, DateTime endsAt, CancellationToken ct = default);

    public Task<UserSubscription> CloneSubscriptionAsync(UserSubscription activeSubscription, Guid linkedSubscriptionId,
        DateTime newStart, DateTime newEnd,
        CancellationToken ct = default);

    public Task<bool> ConfirmSubscriptionAsync(Guid subscriptionId, CancellationToken ct = default);
    public Task<bool> DeleteSubscriptionAsync(Guid subscriptionId, CancellationToken ct = default);

    public Task<bool> SetPaymentAsync(Guid subscriptionId, Guid paymentId, CancellationToken ct = default);
    public Task<IReadOnlyDictionary<Guid, Payment>> GetPaymentsAsync(Guid userId, CancellationToken ct = default);
}