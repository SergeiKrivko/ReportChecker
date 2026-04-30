using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Converters;

public static class UserSubscriptionConverter
{
    public static UserSubscription ToDomain(this UserSubscriptionEntity entity)
    {
        return new UserSubscription
        {
            Id = entity.Id,
            UserId = entity.UserId,
            PlanId = entity.PlanId,
            PaymentId = entity.PaymentId,
            DefaultPricePerMonth = entity.DefaultPricePerMonth,
            Price = entity.Price,
            CreatedAt = entity.CreatedAt,
            ConfirmedAt = entity.ConfirmedAt,
            DeletedAt = entity.DeletedAt,
            StartsAt = entity.StartsAt,
            EndsAt = entity.EndsAt,
        };
    }
}