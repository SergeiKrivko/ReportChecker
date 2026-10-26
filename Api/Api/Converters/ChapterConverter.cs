using ReportChecker.Api.Dtos;
using ReportChecker.Models;

namespace ReportChecker.Api.Converters;

public static class ChapterConverter
{
    public static ChapterDto ToDto(this Chapter chapter)
    {
        return new ChapterDto
        {
            Name = chapter.Name,
            Content = chapter.Content,
            Images = chapter.Images.Select(e => e.ToDto()).ToArray(),
            Children = chapter.Children.Select(e => e.ToDto()).ToArray(),
        };
    }

    public static ChapterImageDto ToDto(this ChapterImage image)
    {
        return new ChapterImageDto
        {
            Data = image.Data,
            MimeType = image.MimeType,
        };
    }
}