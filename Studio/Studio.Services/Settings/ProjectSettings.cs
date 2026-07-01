namespace Studio.Services.Dtos;

internal class ProjectSettings
{
    public required Guid Id { get; init; }
    public string? Name { get; init; }
    public required string Path { get; init; }
    public required string Format { get; init; }
    public Guid? ReportId { get; init; }
}