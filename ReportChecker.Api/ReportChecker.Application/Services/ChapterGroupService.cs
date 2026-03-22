using Microsoft.Extensions.Configuration;
using ReportChecker.Abstractions;
using ReportChecker.Models;

namespace ReportChecker.Application.Services;

public class ChapterGroupService(IConfiguration configuration) : IChapterGroupService
{
    private int MinChapterSize { get; } = int.Parse(configuration["Reports.MinChapterSize"] ?? "50");
    private int MaxRequestSize { get; } = int.Parse(configuration["Reports.MaxRequestSize"] ?? "5000");

    public IEnumerable<Chapter[]> GroupChapters(IEnumerable<Chapter> chapters)
    {
        var lst = new List<Chapter>();
        var length = 0;
        foreach (var chapter in chapters)
        {
            if (chapter.Content.Length < MinChapterSize)
                continue;
            if (length + chapter.Content.Length > MaxRequestSize && lst.Count > 0)
            {
                yield return lst.ToArray();
                lst.Clear();
                length = 0;
            }

            lst.Add(chapter);
            length += chapter.Content.Length;
        }

        if (lst.Count > 0)
            yield return lst.ToArray();
    }

    public IEnumerable<ChapterDifference[]> GroupChapters(IEnumerable<ChapterDifference> chapters)
    {
        var lst = new List<ChapterDifference>();
        var length = 0;
        foreach (var chapter in chapters)
        {
            if (chapter.Difference.Length < MinChapterSize)
                continue;
            if (length + chapter.Difference.Length > MaxRequestSize && lst.Count > 0)
            {
                yield return lst.ToArray();
                lst.Clear();
                length = 0;
            }

            lst.Add(chapter);
            length += chapter.Difference.Length;
        }

        if (lst.Count > 0)
            yield return lst.ToArray();
    }
}