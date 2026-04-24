namespace ReportChecker.Models;

public class CreatedSubscription
{
    public UserSubscription? Subscription { get; init; }
    public decimal UnusedTokensDiscount { get; init; }
    public decimal MonthsDiscount { get; init; }
    public IReadOnlyList<UserSubscription> NextSubscriptions { get; init; } = [];
    public string? ErrorText { get; init; }
}