namespace ReportChecker.Models;

public record ChapterLine(string Content, ChapterLineType Type, int Number);

public enum ChapterLineType
{
    Unchanged,
    Added,
    Deleted,
    Modified
}