namespace ReportChecker.Api.Dtos;

public class UserSubscriptionsDto
{
    public UserSubscriptionDto? Active { get; init; }
    public IReadOnlyList<UserSubscriptionDto> Future { get; init; } = [];
    public DateTime ResetLimitsAt { get; init; }
    public required LimitDto<int> TokensLimit { get; init; }
    public required LimitDto<int> ReportsLimit { get; init; }
}