using ReportChecker.Shared.Models;

namespace ReportChecker.Studio.Abstractions;

public interface IIssueService
{
    public IObservable<IReadOnlyList<Issue>> AllIssues { get; }
    public IObservable<Issue?> SelectedIssue { get; }

    public IObservable<object> Load();

    public void SelectIssue(Issue? issue);
}