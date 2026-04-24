namespace ReportChecker.Api.Schemas;

public class ConfirmSubscriptionSchema
{
    public decimal? Price { get; init; }
    public Guid? UserId { get; init; }
}