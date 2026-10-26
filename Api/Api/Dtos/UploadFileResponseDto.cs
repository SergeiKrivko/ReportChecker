namespace ReportChecker.Api.Dtos;

public class UploadFileResponseDto
{
    public required Guid Id { get; init; }
    public required string FileName { get; init; }
}