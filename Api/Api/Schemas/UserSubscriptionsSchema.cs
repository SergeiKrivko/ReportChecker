using ReportChecker.Models;

namespace ReportChecker.Api.Schemas;

public class UserSubscriptionsSchema
{
    public UserSubscription? Active { get; init; }
    public IReadOnlyList<UserSubscription> Future { get; init; } = [];
    public required Limit<int> TokensLimit { get; init; }
    public required Limit<int> ReportsLimit { get; init; }
}