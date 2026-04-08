namespace ReportChecker.Models;

public class PatchLine
{
    public required int Number { get; init; }
    public string? Content { get; init; }
    public PatchLineType Type { get; init; }
}

public enum PatchLineType
{
    Add,
    Delete,
    Modify
}