using ReportChecker.Api.Dtos;
using ReportChecker.Models;

namespace ReportChecker.Api.Converters;

public static class IssueConverter
{
    public static IssueDto ToDto(this Issue issue)
    {
        return new IssueDto
        {
            Id = issue.Id,
            CheckId = issue.CheckId,
            Title = issue.Title,
            Status = issue.Status,
            Priority = issue.Priority,
            Chapter = issue.Chapter,
            Line = issue.Line,
            Comments = issue.Comments.Select(e => e.ToDto()).ToArray(),
        };
    }
}