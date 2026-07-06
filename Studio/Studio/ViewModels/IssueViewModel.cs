using Avalonia.Controls;
using ReportChecker.Shared.Models;
using ReportChecker.Studio.Abstractions;

namespace ReportChecker.Studio.ViewModels;

public class IssueViewModel(Issue issue, IIssueService issueService) : ViewModelBase
{
    public Issue Issue => issue;

    public string IconKey { get; } = GetIconKey(issue);

    private static string GetIconKey(Issue issue)
    {
        switch (issue.Status)
        {
            case IssueStatus.Open:
                if (issue.Priority >= 1 && issue.Priority <= 2)
                    return "IconShieldAlert";
                if (issue.Priority >= 3 && issue.Priority <= 5)
                    return "IconTriangleAlert";
                return "IconCircleAlert";
            case IssueStatus.Closed:
                return "IconClose";
            case IssueStatus.Fixed:
                return "IconCheckmark";
        }

        return "IconHelp";
    }

    public void SelectIssue()
    {
        issueService.SelectIssue(issue);
    }
}