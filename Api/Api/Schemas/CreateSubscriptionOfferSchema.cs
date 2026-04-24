namespace ReportChecker.Api.Schemas;

public class CreateSubscriptionOfferSchema
{
    public required decimal Price { get; init; }
    public required int Months { get; init; }
}