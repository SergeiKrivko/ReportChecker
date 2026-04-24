using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface ISubscriptionOfferRepository
{
    public Task<IReadOnlyList<SubscriptionOffer>> GetAllOffersAsync(Guid planId, CancellationToken ct = default);
    public Task<SubscriptionOffer?> GetOfferById(Guid offerId, CancellationToken ct = default);
    public Task<Guid> CreateOfferAsync(Guid planId, int months, decimal price, CancellationToken ct = default);
    public Task<bool> UpdateOfferAsync(Guid offerId, int months, decimal price, CancellationToken ct = default);
    public Task<bool> DeleteOfferAsync(Guid offerId, CancellationToken ct = default);
}