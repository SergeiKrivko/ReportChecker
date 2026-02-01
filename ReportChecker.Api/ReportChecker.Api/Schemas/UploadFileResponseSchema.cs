namespace ReportChecker.Api.Schemas;

public class UploadFileResponseSchema
{
    public required Guid Id { get; init; }
    public required string FileName { get; init; }
}