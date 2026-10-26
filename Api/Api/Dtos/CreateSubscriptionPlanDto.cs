namespace ReportChecker.Api.Dtos;

public class CreateSubscriptionPlanDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required int TokensLimit { get; init; }
    public required int ReportsLimit { get; init; }
    public bool IsHidden { get; init; }
}