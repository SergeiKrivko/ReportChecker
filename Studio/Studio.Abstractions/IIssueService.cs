using ReportChecker.Shared.Models;

namespace ReportChecker.Studio.Abstractions;

public interface IIssueService
{
    public IObservable<IReadOnlyList<FileIssue>> AllIssues { get; }
    public IObservable<FileIssue?> SelectedIssue { get; }

    public IObservable<object> Load();
    public Task ReloadIssues();

    public void SelectIssue(FileIssue? issue);
}