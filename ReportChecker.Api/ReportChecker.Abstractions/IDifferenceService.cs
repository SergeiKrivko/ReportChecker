using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IDifferenceService
{
    public IEnumerable<ChapterDifference> GetDifference(IEnumerable<Chapter> newChapters,
        IEnumerable<Chapter> oldChapters);
    public ChapterDifference GetDifference(Chapter newChapter, Chapter? oldChapter);
}