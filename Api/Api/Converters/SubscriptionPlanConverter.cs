using ReportChecker.Api.Dtos;
using ReportChecker.Models;

namespace ReportChecker.Api.Converters;

public static class SubscriptionPlanConverter
{
    public static SubscriptionPlanDto ToDto(this SubscriptionPlan plan)
    {
        return new SubscriptionPlanDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            TokensLimit = plan.TokensLimit,
            ReportsLimit = plan.ReportsLimit,
            IsHidden = plan.IsHidden,
            Offers = plan.Offers.Select(e => e.ToDto()).ToList(),
            CreatedAt = plan.CreatedAt,
            DeletedAt = plan.DeletedAt,
        };
    }
}