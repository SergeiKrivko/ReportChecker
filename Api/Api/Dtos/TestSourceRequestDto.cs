namespace ReportChecker.Api.Dtos;

public class TestSourceRequestDto
{
    public required string Provider { get; init; }
    public required ReportSourceUnionDto Source { get; init; }
}