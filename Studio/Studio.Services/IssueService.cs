using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReportChecker.Shared.ApiClient;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Services.Converters;
using Issue = ReportChecker.Shared.Models.Issue;

namespace ReportChecker.Studio.Services;

public class IssueService(IReportService reportService, IApiClient apiClient) : IIssueService
{
    private readonly BehaviorSubject<IReadOnlyList<Issue>> _allIssues = new([]);
    public IObservable<IReadOnlyList<Issue>> AllIssues => _allIssues;
    private readonly BehaviorSubject<Issue?> _selectedIssue = new(null);
    public IObservable<Issue?> SelectedIssue => _selectedIssue;

    public IObservable<object> Load()
    {
        return reportService.CurrentReport
            .Select(report => Observable.FromAsync(ct => report == null
                ? Task.FromResult<ICollection<ReportChecker.Shared.ApiClient.Issue>>([])
                : apiClient.IssuesAllAsync(report.Id, ct)))
            .Concat()
            .Do(e => _allIssues.OnNext(e.Select(i => i.ToDomain()).ToList()));
    }

    public void SelectIssue(Issue? issue)
    {
        _selectedIssue.OnNext(issue);
    }
}