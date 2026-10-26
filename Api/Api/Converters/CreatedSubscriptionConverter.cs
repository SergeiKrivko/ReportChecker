using ReportChecker.Api.Dtos;
using ReportChecker.Models;

namespace ReportChecker.Api.Converters;

public static class CreatedSubscriptionConverter
{
    public static CreatedSubscriptionDto ToDto(this CreatedSubscription subscription)
    {
        return new CreatedSubscriptionDto
        {
            Subscription = subscription.Subscription?.ToDto(),
            UnusedTokensDiscount = subscription.UnusedTokensDiscount,
            MonthsDiscount = subscription.MonthsDiscount,
            NextSubscriptions = subscription.NextSubscriptions.Select(e => e.ToDto()).ToList(),
        };
    }
}