namespace ReportChecker.Api.Dtos;

public class CreateCheckDto
{
    public CheckSourceUnionDto? Source { get; init; }
    public string? Name { get; init; }
}