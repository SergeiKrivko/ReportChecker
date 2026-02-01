namespace ReportChecker.SourceProviders.File;

public class FileSourceSchema
{
    public required Guid Id { get; init; }
    public required string FileName { get; init; }
}