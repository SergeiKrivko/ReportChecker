using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Converters;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Repositories;

public class UserSubscriptionRepository(ReportCheckerDbContext dbContext) : IUserSubscriptionRepository
{
    public async Task<UserSubscription?> GetSubscriptionByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await dbContext.UserSubscriptions
            .Where(e => e.Id == id)
            .FirstOrDefaultAsync(ct);
        return entity?.ToDomain();
    }

    public async Task<UserSubscription?> GetActiveSubscriptionAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var entity = await dbContext.UserSubscriptions
            .Where(e => e.UserId == userId && e.DeletedAt == null && e.ConfirmedAt != null && e.StartsAt < now &&
                        e.EndsAt > now)
            .FirstOrDefaultAsync(ct);
        return entity?.ToDomain();
    }

    public async Task<UserSubscription?> GetLastSubscriptionAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var entity = await dbContext.UserSubscriptions
            .Where(e => e.UserId == userId && e.DeletedAt == null && e.ConfirmedAt != null && e.StartsAt < now)
            .OrderByDescending(e => e.EndsAt)
            .FirstOrDefaultAsync(ct);
        return entity?.ToDomain();
    }

    public async Task<IReadOnlyList<UserSubscription>> GetFutureSubscriptionsAsync(Guid userId,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var entities = await dbContext.UserSubscriptions
            .Where(e => e.UserId == userId && e.DeletedAt == null && e.ConfirmedAt != null && e.StartsAt > now)
            .OrderBy(e => e.StartsAt)
            .ToListAsync(ct);
        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task<UserSubscription> CreateSubscriptionAsync(Guid planId, Guid userId, decimal defaultPrice,
        decimal price,
        DateTime startsAt,
        DateTime endsAt, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        var entity = new UserSubscriptionEntity
        {
            Id = id,
            UserId = userId,
            PlanId = planId,
            LinkedSubscriptionId = null,
            ParentSubscriptionId = null,
            DefaultPricePerMonth = defaultPrice,
            Price = price,
            StartsAt = startsAt,
            EndsAt = endsAt,
            CreatedAt = DateTime.UtcNow,
            DeletedAt = null,
            ConfirmedAt = null,
        };
        await dbContext.UserSubscriptions.AddAsync(entity, ct);
        await dbContext.SaveChangesAsync(ct);
        return entity.ToDomain();
    }

    public async Task<UserSubscription> CloneSubscriptionAsync(UserSubscription activeSubscription,
        Guid linkedSubscriptionId,
        DateTime newStart, DateTime newEnd,
        CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        var entity = new UserSubscriptionEntity
        {
            Id = id,
            UserId = activeSubscription.UserId,
            PlanId = activeSubscription.PlanId,
            LinkedSubscriptionId = linkedSubscriptionId,
            ParentSubscriptionId = activeSubscription.Id,
            DefaultPricePerMonth = activeSubscription.DefaultPricePerMonth,
            Price = activeSubscription.Price,
            StartsAt = newStart,
            EndsAt = newEnd,
            CreatedAt = DateTime.UtcNow,
            DeletedAt = null,
            ConfirmedAt = null,
        };
        await dbContext.UserSubscriptions.AddAsync(entity, ct);
        await dbContext.SaveChangesAsync(ct);
        return entity.ToDomain();
    }

    public async Task<bool> ConfirmSubscriptionAsync(Guid subscriptionId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var subscription = await GetSubscriptionByIdAsync(subscriptionId, ct);
        if (subscription == null)
            throw new Exception("Subscription not found");
        var subscriptions = await dbContext.UserSubscriptions
            .Where(e => e.Id == subscriptionId || e.LinkedSubscriptionId == subscriptionId)
            .ToListAsync(ct);
        var subscriptionIds = subscriptions.Select(e => e.Id).ToList();

        var parentSubscriptionIds = subscriptions.Select(e => e.ParentSubscriptionId)
            .Where(e => e != null)
            .Select(e => e!.Value)
            .ToList();

        await dbContext.UserSubscriptions
            .Where(e => parentSubscriptionIds.Contains(e.Id))
            .ExecuteUpdateAsync(p => p.SetProperty(e => e.DeletedAt, now), ct);
        await dbContext.UserSubscriptions
            .Where(e => e.DeletedAt == null && e.ConfirmedAt != null && e.EndsAt > subscription.StartsAt)
            .ExecuteUpdateAsync(p => p.SetProperty(e => e.DeletedAt, now), ct);

        var count = await dbContext.UserSubscriptions
            .Where(e => subscriptionIds.Contains(e.Id))
            .ExecuteUpdateAsync(p => p.SetProperty(e => e.ConfirmedAt, now), ct);

        await dbContext.UserSubscriptions
            .Where(e => e.UserId == subscriptions[0].UserId && e.ConfirmedAt == null && e.DeletedAt != null)
            .ExecuteUpdateAsync(p => p.SetProperty(e => e.DeletedAt, DateTime.UtcNow), ct);

        await dbContext.SaveChangesAsync(ct);
        return count > 0;
    }

    public async Task<bool> DeleteSubscriptionAsync(Guid subscriptionId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var count = await dbContext.UserSubscriptions
            .Where(e => e.Id == subscriptionId || e.LinkedSubscriptionId == subscriptionId)
            .ExecuteUpdateAsync(p => p.SetProperty(e => e.DeletedAt, now), ct);
        await dbContext.SaveChangesAsync(ct);
        return count > 0;
    }

    public async Task<bool> SetPaymentAsync(Guid subscriptionId, Guid paymentId, CancellationToken ct = default)
    {
        var count = await dbContext.UserSubscriptions
            .Where(e => e.Id == subscriptionId)
            .ExecuteUpdateAsync(p => p.SetProperty(e => e.PaymentId, paymentId), ct);
        await dbContext.SaveChangesAsync(ct);
        return count > 0;
    }

    public async Task<IReadOnlyDictionary<Guid, Payment>> GetPaymentsAsync(Guid userId, CancellationToken ct = default)
    {
        var entities = await dbContext.UserSubscriptions
            .Where(e => e.UserId == userId && e.DeletedAt == null && e.ConfirmedAt == null)
            .Where(e => e.PaymentId != null)
            .Include(e => e.Payment)
            .ToListAsync(ct);
        return entities.ToDictionary(e => e.Id, e => e.Payment!.ToDomain());
    }
}