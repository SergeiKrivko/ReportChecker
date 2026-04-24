using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Converters;

public static class SubscriptionOfferConverter
{
    public static SubscriptionOffer ToDomain(this SubscriptionOfferEntity entity)
    {
        return new SubscriptionOffer
        {
            Id = entity.Id,
            PlanId = entity.PlanId,
            Months = entity.Months,
            Price = entity.Price,
            CreatedAt = entity.CreatedAt,
            DeletedAt = entity.DeletedAt,
        };
    }
}