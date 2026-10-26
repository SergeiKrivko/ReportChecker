using ReportChecker.Api.Dtos;
using ReportChecker.Models;

namespace ReportChecker.Api.Converters;

public static class UserSubscriptionConverter
{
    public static UserSubscriptionDto ToDto(this UserSubscription subscription)
    {
        return new UserSubscriptionDto
        {
            Id = subscription.Id,
            PlanId = subscription.PlanId,
            UserId = subscription.UserId,
            PaymentId = subscription.PaymentId,
            DefaultPricePerMonth = subscription.DefaultPricePerMonth,
            Price = subscription.Price,
            CreatedAt = subscription.CreatedAt,
            ConfirmedAt = subscription.ConfirmedAt,
            DeletedAt = subscription.DeletedAt,
            StartsAt = subscription.StartsAt,
            EndsAt = subscription.EndsAt,
        };
    }
}