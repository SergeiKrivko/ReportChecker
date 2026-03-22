using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IDifferenceService
{
    public IEnumerable<ChapterDifference> GetDifference(IEnumerable<Chapter> oldChapters,
        IEnumerable<Chapter> newChapters);
    public ChapterDifference GetDifference(Chapter? oldChapter, Chapter newChapter);
}