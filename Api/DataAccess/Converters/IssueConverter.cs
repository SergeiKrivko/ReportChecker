using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Converters;

public static class IssueConverter
{
    public static Issue ToDomain(this IssueEntity entity, Guid? userId = null)
    {
        var location = entity.Locations
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefault();
        return new Issue
        {
            Id = entity.IssueId,
            CheckId = entity.CheckId,
            Title = entity.Title,
            Priority = entity.Priority,
            Status = entity.Comments
                .Where(e => e.Status != null)
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefault()
                ?.Status ?? IssueStatus.Open,
            Comments = entity.Comments
                .Where(e => e.DeletedAt == null)
                .OrderBy(e => e.CreatedAt)
                .Select(e => e.ToDomain(userId))
                .ToArray(),
            Chapter = location?.Chapter ?? "???",
            Line = location?.Line,
        };
    }
}