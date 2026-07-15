using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReportChecker.Shared.ApiClient;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Services.Converters;
using Comment = ReportChecker.Shared.Models.Comment;
using Issue = ReportChecker.Shared.Models.Issue;
using IssueStatus = ReportChecker.Shared.Models.IssueStatus;
using ProgressStatus = ReportChecker.Shared.Models.ProgressStatus;
using Report = ReportChecker.Shared.Models.Report;

namespace ReportChecker.Studio.Services;

public class CommentsService(IIssueService issueService, IApiClient apiClient, IReportService reportService)
    : ICommentsService
{
    private readonly BehaviorSubject<IReadOnlyList<Comment>> _allComments = new([]);
    public IObservable<IReadOnlyList<Comment>> AllComments => _allComments;

    public IObservable<object?> Load()
    {
        return issueService.SelectedIssue
            .Select(issue => Observable.FromAsync(ct => LoadComments(issue, ct)))
            .Concat()
            .Do(e => _allComments.OnNext(e));
    }

    private async Task<IReadOnlyList<Comment>> LoadComments(Issue? issue, CancellationToken ct)
    {
        var report = await reportService.CurrentReport.FirstAsync();
        if (report == null)
            return [];
        return await LoadComments(report, issue, ct);
    }

    private async Task<IReadOnlyList<Comment>> LoadComments(Report report, Issue? issue, CancellationToken ct)
    {
        if (issue == null)
            return [];
        var resp = await apiClient.CommentsAllAsync(report.Id, issue.Id, ct);
        return resp
            .OrderBy(e => e.CreatedAt)
            .Select(e => e.ToDomain())
            .ToList();
    }

    public async Task CreateComment(string content, CancellationToken ct = default)
    {
        var report = await reportService.CurrentReport.FirstAsync();
        var issue = await issueService.SelectedIssue.FirstAsync();
        if (report == null || issue == null)
            return;
        await apiClient.CommentsPOSTAsync(report.Id, issue.Id, new CreateCommentSchema
        {
            Content = content
        }, ct);
        var comments = await LoadComments(report, issue, ct);
        _allComments.OnNext(comments);
        await StartPolling(report.Id, issue.Id);
    }

    public async Task CreateComment(IssueStatus status, CancellationToken ct = default)
    {
        var report = await reportService.CurrentReport.FirstAsync();
        var issue = await issueService.SelectedIssue.FirstAsync();
        if (report == null || issue == null)
            return;
        await apiClient.CommentsPOSTAsync(report.Id, issue.Id, new CreateCommentSchema
        {
            Status = status.ToDto(),
        }, ct);
        var comments = await LoadComments(report, issue, ct);
        _allComments.OnNext(comments);
        await StartPolling(report.Id, issue.Id);
    }

    private CancellationTokenSource? _pollingCtSource;

    private async Task StartPolling(Guid reportId, Guid issueId)
    {
        if (_pollingCtSource != null)
            await _pollingCtSource.CancelAsync();
        _pollingCtSource = new CancellationTokenSource();
        RunPolling(reportId, issueId, _pollingCtSource.Token);
        _pollingCtSource = null;
    }

    private async void RunPolling(Guid reportId, Guid issueId, CancellationToken ct)
    {
        try
        {
            var comments = await apiClient.CommentsAllAsync(reportId, issueId, ct);
            var comment = comments
                .Where(e => e.UserId == Guid.Empty)
                .MaxBy(e => e.CreatedAt)?.ToDomain();
            Console.WriteLine(comment?.Content);
            Console.WriteLine(comment?.ProgressStatus);
            if (comment == null)
                return;
            while(comment.ProgressStatus != ProgressStatus.Completed &&
                  comment.ProgressStatus != ProgressStatus.Failed
                  && !ct.IsCancellationRequested)
            {
                await Task.Delay(1000, ct);
                var resp = await apiClient.CommentsGETAsync(reportId, issueId, comment.Id, ct);
                Console.WriteLine(resp.ProgressStatus);
                comment = resp.ToDomain();
            };

            var newComments = (await _allComments.FirstAsync())
                .Select(e => e.Id == comment.Id ? comment : e)
                .ToList();
            _allComments.OnNext(newComments);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}