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

    private string GetDifference(string oldText, string newText)
    {
        var diff = _diffBuilder.BuildDiffModel(oldText, newText);

        var result = new StringBuilder();
        foreach (var line in diff.Lines)
        {
            switch (line.Type)
            {
                case ChangeType.Inserted:
                    result.AppendLine($"+ {line.Text}");
                    break;
                case ChangeType.Deleted:
                    result.AppendLine($"- {line.Text}");
                    break;
                case ChangeType.Modified:
                    result.AppendLine($"* {line.Text}");
                    break;
                default:
                    result.AppendLine($"  {line.Text}");
                    break;
            }
        }

        return result.ToString();
    }
}