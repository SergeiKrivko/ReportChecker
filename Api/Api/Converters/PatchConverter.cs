using ReportChecker.Api.Dtos;
using ReportChecker.Models;

namespace ReportChecker.Api.Converters;

public static class PatchConverter
{
    public static PatchDto ToDto(this Patch patch)
    {
        return new PatchDto
        {
            Id = patch.Id,
            CommentId = patch.CommentId,
            Status = patch.Status,
            Lines = patch.Lines.Select(e => e.ToDto()).ToList(),
            CreatedAt = patch.CreatedAt,
        };
    }

    public static PatchLineDto ToDto(this PatchLine line)
    {
        return new PatchLineDto
        {
            Number = line.Number,
            Content = line.Content,
            PreviousContent = line.PreviousContent,
            Type = line.Type,
        };
    }
}