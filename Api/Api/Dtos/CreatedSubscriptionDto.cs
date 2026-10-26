namespace ReportChecker.Api.Dtos;

public class CreatedSubscriptionDto
{
    public UserSubscriptionDto? Subscription { get; init; }
    public decimal UnusedTokensDiscount { get; init; }
    public decimal MonthsDiscount { get; init; }
    public IReadOnlyList<UserSubscriptionDto> NextSubscriptions { get; init; } = [];
}