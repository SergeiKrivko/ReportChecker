namespace ReportChecker.Shared.Models;

public class FilePatch
{
    public required string Path { get; init; }
    public IReadOnlyList<PatchLine> Lines { get; init; } = [];
    public bool IsHandled { get; set; }
}