using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.Services.Converters;

public static class SubscriptionConverter
{
    public static UserSubscription ToDomain(this Shared.ApiClient.UserSubscription dto)
    {
        return new UserSubscription
        {
            Id = dto.Id,
            UserId = dto.UserId,
            PlanId = dto.Id,
            CreatedAt = dto.CreatedAt,
            DeletedAt = dto.DeletedAt,
            ConfirmedAt = dto.ConfirmedAt,
            StartsAt = dto.StartsAt,
            EndsAt = dto.EndsAt,
        };
    }

    public static CurrentSubscription ToDomain(this Shared.ApiClient.UserSubscriptionsSchema dto)
    {
        return new CurrentSubscription
        {
            Active = dto.Active?.ToDomain(),
            ResetLimitsAt = dto.ResetLimitsAt ?? DateTimeOffset.Now,
            ReportsLimit = new Limit<int>
            {
                Current = dto.ReportsLimit.Current,
                Maximum = dto.ReportsLimit.Maximum,
            },
            TokensLimit = new Limit<int>
            {
                Current = dto.TokensLimit.Current,
                Maximum = dto.TokensLimit.Maximum,
            },
        };
    }

    public static SubscriptionPlan ToDomain(this Shared.ApiClient.SubscriptionPlan dto)
    {
        return new SubscriptionPlan()
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description,
            ReportsLimit = dto.ReportsLimit ?? 0,
            TokensLimit = dto.TokensLimit ?? 0,
        };
    }
}