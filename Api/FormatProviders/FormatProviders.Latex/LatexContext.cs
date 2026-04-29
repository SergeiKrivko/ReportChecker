using ReportChecker.Abstractions;

namespace ReportChecker.FormatProviders.Latex;

internal class LatexContext
{
    public required IFileArchive Archive { get; init; }
    public string? ImagesPath { get; set; }
    public string? FileName { get; set; }
}