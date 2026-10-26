using ReportChecker.Api.Dtos;
using ReportChecker.Models;

namespace ReportChecker.Api.Converters;

public static class SubscriptionOfferConverter
{
    public static SubscriptionOfferDto ToDto(this SubscriptionOffer offer)
    {
        return new SubscriptionOfferDto
        {
            Id = offer.Id,
            PlanId = offer.PlanId,
            Months = offer.Months,
            Price = offer.Price,
            CreatedAt = offer.CreatedAt,
            DeletedAt = offer.DeletedAt,
        };
    }
}