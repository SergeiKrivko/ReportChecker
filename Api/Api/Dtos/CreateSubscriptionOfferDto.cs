namespace ReportChecker.Api.Dtos;

public class CreateSubscriptionOfferDto
{
    public required decimal Price { get; init; }
    public required int Months { get; init; }
}