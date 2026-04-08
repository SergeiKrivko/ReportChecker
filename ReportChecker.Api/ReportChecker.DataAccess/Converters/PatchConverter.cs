using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Converters;

public static class PatchConverter
{
    public static Patch ToDomain(this PatchEntity entity)
    {
        return new Patch
        {
            Id = entity.Id,
            CommentId = entity.CommentId,
            Status = entity.Status,
            Lines = entity.Lines
                .OrderBy(l => l.Index)
                .Select(ToDomain).ToList(),
            CreatedAt = entity.CreatedAt,
        };
    }

    public static PatchLine ToDomain(this PatchLineEntity entity)
    {
        return new PatchLine
        {
            Number = entity.Number,
            Content = entity.Content,
            Type = entity.Type
        };
    }
}