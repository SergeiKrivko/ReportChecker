using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IChapterGroupService
{
    public IEnumerable<Chapter[]> GroupChapters(IEnumerable<Chapter> chapters);
    public IEnumerable<ChapterDifference[]> GroupChapters(IEnumerable<ChapterDifference> chapters);
}