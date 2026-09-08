using ReportChecker.Abstractions;
using ReportChecker.Models;

namespace ReportChecker.Application.Services;

public class PositionsUpdater(IDifferenceService differenceService, IIssueRepository issueRepository)
    : IPositionsUpdater
{
    public async Task UpdatePositionsAsync(CheckContext context, CancellationToken ct = default)
    {
        foreach (var newChapter in context.NewChapters)
        {
            var oldChapter = context.OldChapters.FirstOrDefault(e => e.Name == newChapter.Name);
            var diff = differenceService.GetDifference(newChapter, oldChapter);
            var issues = context.Issues
                .Where(e => e.Chapter == newChapter.Name)
                .OrderBy(e => e.Line)
                .ToList();
            var oldLine = 0;
            var newLine = 0;
            foreach (var diffLine in diff.Difference)
            {
                if (diffLine.Type != ChapterLineType.Added)
                    oldLine++;
                if (diffLine.Type != ChapterLineType.Deleted)
                    newLine++;
                while (issues[0].Line == oldLine)
                {
                    if (issues[0].Line != newLine)
                        await issueRepository.UpdateIssueLocationAsync(issues[0].Id, context.Check.Id, newChapter.Name,
                            newLine, ct);
                    issues.RemoveAt(0);
                }
            }
        }
    }
}