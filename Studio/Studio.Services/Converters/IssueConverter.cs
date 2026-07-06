using ReportChecker.Shared.Models;

namespace ReportChecker.Studio.Services.Converters;

public static class IssueConverter
{
    public static Issue ToDomain(this ReportChecker.Shared.ApiClient.Issue dto)
    {
        return new Issue
        {
            Id = dto.Id,
            CheckId = dto.Id,
            Title = dto.Title,
            Status = dto.Status?.ToDomain(),
            Priority = dto.Priority,
            Chapter = dto.Chapter,
            Comments = dto.Comments?.Select(e => e.ToDomain()).ToList() ?? [],
        };
    }

    public static IssueStatus ToDomain(this ReportChecker.Shared.ApiClient.IssueStatus dtoStatus)
    {
        return dtoStatus switch
        {
            Shared.ApiClient.IssueStatus.Open => IssueStatus.Open,
            Shared.ApiClient.IssueStatus.Closed => IssueStatus.Closed,
            Shared.ApiClient.IssueStatus.Fixed => IssueStatus.Fixed,
            _ => IssueStatus.Open,
        };
    }
}