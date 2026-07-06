using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReportChecker.Shared.ApiClient;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Services.Converters;
using Comment = ReportChecker.Shared.Models.Comment;
using Issue = ReportChecker.Shared.Models.Issue;

namespace ReportChecker.Studio.Services;

public class CommentsService(IIssueService issueService, IApiClient apiClient, IReportService reportService) : ICommentsService
{
    private readonly BehaviorSubject<IReadOnlyList<Comment>> _allComments = new([]);
    public IObservable<IReadOnlyList<Comment>> AllComments => _allComments;

    public IObservable<object> Load()
    {
        return issueService.SelectedIssue
            .Select(issue => Observable.FromAsync(ct => LoadComments(issue, ct)))
            .Concat()
            .Do(e => _allComments.OnNext(e));
    }

    private async Task<IReadOnlyList<Comment>> LoadComments(Issue? issue, CancellationToken ct)
    {
        if (issue == null)
            return [];
        var report = await reportService.CurrentReport.FirstAsync();
        if (report == null)
            return [];
        var resp = await apiClient.CommentsAllAsync(report.Id, issue.Id, ct);
        return resp.Select(e => e.ToDomain()).ToList();
    }
}