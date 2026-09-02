using Lucide.Avalonia;
using ReportChecker.Shared.Models;
using ReportChecker.Studio.Abstractions;

namespace ReportChecker.Studio.ViewModels;

public class IssueViewModel(FileIssue issue, IIssueService issueService) : ViewModelBase
{
    public FileIssue Issue => issue;

    public LucideIconKind IconKey { get; } = GetIconKey(issue.Issue);

    private static LucideIconKind GetIconKey(Issue issue)
    {
        switch (issue.Status)
        {
            case IssueStatus.Open:
                if (issue.Priority >= 1 && issue.Priority <= 2)
                    return LucideIconKind.ShieldAlert;
                if (issue.Priority >= 3 && issue.Priority <= 5)
                    return LucideIconKind.TriangleAlert;
                return LucideIconKind.CircleAlert;
            case IssueStatus.Closed:
                return LucideIconKind.X;
            case IssueStatus.Fixed:
                return LucideIconKind.Check;
        }

        return LucideIconKind.OctagonAlert;
    }

    public void SelectIssue()
    {
        issueService.SelectIssue(issue);
    }
}