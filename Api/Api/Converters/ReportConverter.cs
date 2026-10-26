using ReportChecker.Api.Dtos;
using ReportChecker.Models;

namespace ReportChecker.Api.Converters;

public static class ReportConverter
{
    public static ReportDto ToDto(this Report report)
    {
        return new ReportDto
        {
            Id = report.Id,
            OwnerId = report.OwnerId,
            Name = report.Name,
            SourceProvider = report.SourceProvider,
            Format = report.Format,
            LlmModelId = report.LlmModelId,
            ImageProcessingMode = report.ImageProcessingMode,
            ReasoningLevel = report.ReasoningLevel,
            CreatedAt = report.CreatedAt,
            DeletedAt = report.DeletedAt,
            Source = report.Source?.ToDto(),
            IssueCount = report.IssueCount,
            UpdatedAt = report.UpdatedAt,
        };
    }
}