namespace ReportChecker.Models;

public class ModelPrice
{
    public required string ModelId { get; init; }

    public required decimal InputRubPerMillion { get; init; }
    public required decimal OutputRubPerMillion { get; init; }

    public decimal InputCoefficient => InputRubPerMillion * 10000;
    public decimal OutputCoefficient => InputRubPerMillion * 10000;
}