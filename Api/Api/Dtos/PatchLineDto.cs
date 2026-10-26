using ReportChecker.Models;

namespace ReportChecker.Api.Dtos;

public class PatchLineDto
{
    public required int Number { get; init; }
    public string? Content { get; init; }
    public string? PreviousContent { get; init; }
    public PatchLineType Type { get; init; }
}