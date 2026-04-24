using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Converters;

public static class SubscriptionPlanConverter
{
    public static SubscriptionPlan ToDomain(this SubscriptionPlanEntity entity)
    {
        return new SubscriptionPlan
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            TokensLimit = entity.TokensLimit,
            ReportsLimit = entity.ReportsLimit,
            IsHidden = entity.IsHidden,
            CreatedAt = entity.CreatedAt,
            DeletedAt = entity.DeletedAt,
            Offers = entity.Offers.OrderBy(e => e.Price).Select(e => e.ToDomain()).ToList(),
        };
    }
}