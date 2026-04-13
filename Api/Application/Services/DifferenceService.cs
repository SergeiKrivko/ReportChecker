using System.Text;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using ReportChecker.Abstractions;
using ReportChecker.Models;

namespace ReportChecker.Application.Services;

public class DifferenceService : IDifferenceService
{
    private readonly InlineDiffBuilder _diffBuilder = new(new Differ());

    public IEnumerable<ChapterDifference> GetDifference(IEnumerable<Chapter> newChapters,
        IEnumerable<Chapter> oldChapters)
    {
        oldChapters = oldChapters.ToList();
        foreach (var newChapter in newChapters)
        {
            var oldChapter = oldChapters.FirstOrDefault(e => e.Name == newChapter.Name);
            yield return GetDifference(newChapter, oldChapter);
        }
    }

    public ChapterDifference GetDifference(Chapter newChapter, Chapter? oldChapter)
    {
        if (oldChapter != null && oldChapter.Name != newChapter.Name)
            throw new InvalidOperationException("Different chapters are not supported.");
        return new ChapterDifference
        {
            Name = newChapter.Name,
            OldContent = oldChapter?.Content,
            NewContent = newChapter.Content,
            Difference = GetDifference(oldChapter?.Content ?? "", newChapter.Content),
        };
    }

    private List<ChapterLine> GetDifference(string oldText, string newText)
    {
        var diff = _diffBuilder.BuildDiffModel(oldText, newText);
        List<ChapterLine> lines = [];
        var index = 0;

        foreach (var line in diff.Lines)
        {
            if (line.Type != ChangeType.Deleted)
                index++;
            lines.Add(new ChapterLine(line.Text, line.Type switch
            {
                ChangeType.Inserted => ChapterLineType.Added,
                ChangeType.Deleted => ChapterLineType.Deleted,
                ChangeType.Modified => ChapterLineType.Modified,
                _ => ChapterLineType.Unchanged,
            }, index));
        }

        return lines;
    }
}