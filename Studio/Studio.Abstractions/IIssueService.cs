using ReportChecker.Shared.Models;
using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.Abstractions;

public interface IIssueService
{
    public IObservable<LoadingStatus> Loading { get; }
    public IObservable<IReadOnlyList<FileIssue>> AllIssues { get; }
    public IObservable<FileIssue?> SelectedIssue { get; }

    public IObservable<object> Load();
    public Task ReloadIssues();

    public void SelectIssue(FileIssue? issue);
    public IReadOnlyList<FileIssue> UpdateIssuePositions(IEnumerable<FileIssue> source, string oldText, string newText);
    public Task MarkRead(Guid issueId, CancellationToken ct = default);
}