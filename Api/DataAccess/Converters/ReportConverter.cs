using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Converters;

public static class ReportConverter
{
    public static Report ToDomain(this ReportEntity entity)
    {
        return new Report
        {
            Id = entity.ReportId,
            OwnerId = entity.OwnerId,
            Name = entity.Name,
            SourceProvider = entity.SourceProvider,
            Format = entity.Format,
            LlmModelId = entity.LlmModelId,
            ImageProcessingMode = entity.ImageProcessingMode,
            ReasoningLevel = entity.ReasoningLevel,
            CreatedAt = entity.CreatedAt,
            DeletedAt = entity.DeletedAt,

            IssueCount = GetIssueCount(entity),
            UpdatedAt = GetUpdatedAt(entity),
            Source = new ReportSourceUnion
            {
                File = entity.FileSource?.ToDomain(),
                GitHub = entity.GitHubSource?.ToDomain(),
                Local = entity.LocalSource?.ToDomain(),
            },
        };
    }

    private static Dictionary<int, int> GetIssueCount(ReportEntity entity)
    {
        return entity.Checks
            .SelectMany(e => e.Issues)
            .GroupBy(e => e.Priority)
            .ToDictionary(e => e.Key,
                e => e.Count(i => i.Comments
                    .Where(c => c.Status != null)
                    .OrderByDescending(c => c.CreatedAt)
                    .First().Status == IssueStatus.Open));
    }

    private static DateTime GetUpdatedAt(ReportEntity entity)
    {
        try
        {
            return entity.Checks.SelectMany(e =>
                    e.Issues.SelectMany(i => i.Comments.Select(c => c.CreatedAt)))
                .Max();
        }
        catch (InvalidOperationException)
        {
            return entity.CreatedAt;
        }
    }
}