using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Converters;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Repositories;

public class SubscriptionOfferRepository(ReportCheckerDbContext dbContext) : ISubscriptionOfferRepository
{
    public async Task<IReadOnlyList<SubscriptionOffer>> GetAllOffersAsync(Guid planId, CancellationToken ct = default)
    {
        var entities = await dbContext.SubscriptionOffers
            .Where(e => e.PlanId == planId && e.DeletedAt == null)
            .ToListAsync(ct);
        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task<SubscriptionOffer?> GetOfferById(Guid offerId, CancellationToken ct = default)
    {
        var entity = await dbContext.SubscriptionOffers
            .Where(e => e.Id == offerId)
            .FirstOrDefaultAsync(ct);
        return entity?.ToDomain();
    }

    public async Task<Guid> CreateOfferAsync(Guid planId, int months, decimal price, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        var entity = new SubscriptionOfferEntity
        {
            Id = id,
            PlanId = planId,
            Months = months,
            Price = price,
            CreatedAt = DateTime.UtcNow,
            DeletedAt = null,
        };
        await dbContext.SubscriptionOffers.AddAsync(entity, ct);
        await dbContext.SaveChangesAsync(ct);
        return id;
    }

    public async Task<bool> UpdateOfferAsync(Guid offerId, int months, decimal price, CancellationToken ct = default)
    {
        var count = await dbContext.SubscriptionOffers
            .Where(e => e.Id == offerId)
            .ExecuteUpdateAsync(p => p
                    .SetProperty(e => e.Months, months)
                    .SetProperty(e => e.Price, price)
                , ct);
        await dbContext.SaveChangesAsync(ct);
        return count > 0;
    }

    public async Task<bool> DeleteOfferAsync(Guid offerId, CancellationToken ct = default)
    {
        var count = await dbContext.SubscriptionOffers
            .Where(e => e.Id == offerId)
            .ExecuteUpdateAsync(p => p
                    .SetProperty(e => e.DeletedAt, DateTime.UtcNow)
                , ct);
        await dbContext.SaveChangesAsync(ct);
        return count > 0;
    }
}