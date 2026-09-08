namespace ReportChecker.Models;

public class LLmModelPrice
{
    public required string ModelId { get; init; }

    public required decimal InputRubPerMillion { get; init; }
    public required decimal OutputRubPerMillion { get; init; }

    public decimal InputCoefficient => InputRubPerMillion / 100;
    public decimal OutputCoefficient => OutputRubPerMillion / 100;
}