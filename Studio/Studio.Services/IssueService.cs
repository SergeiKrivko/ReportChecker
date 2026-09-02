using System.Reactive.Linq;
using System.Reactive.Subjects;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using ReportChecker.Shared.ApiClient;
using ReportChecker.Shared.Models;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Models;
using ReportChecker.Studio.Services.Converters;
using Issue = ReportChecker.Shared.Models.Issue;
using Report = ReportChecker.Shared.Models.Report;

namespace ReportChecker.Studio.Services;

public class IssueService(
    IReportService reportService,
    IApiClient apiClient,
    IProjectService projectService,
    IAlertService alertService,
    ICacheService cacheService)
    : IIssueService
{
    private readonly BehaviorSubject<LoadingStatus> _loading = new(LoadingStatus.NotLoaded);
    public IObservable<LoadingStatus> Loading => _loading;
    private readonly BehaviorSubject<IReadOnlyList<FileIssue>> _allIssues = new([]);
    public IObservable<IReadOnlyList<FileIssue>> AllIssues => _allIssues;
    private readonly BehaviorSubject<FileIssue?> _selectedIssue = new(null);
    public IObservable<FileIssue?> SelectedIssue => _selectedIssue;

    public IObservable<object> Load()
    {
        return reportService.CurrentReport
            .Select<Report?, Task<LoadingStatus>>(report => ReloadIssuesWhileFromCache(report))
            .Switch()
            .Select<LoadingStatus, object>(e => e);
    }

    public async Task ReloadIssues()
    {
        var report = await reportService.CurrentReport.FirstAsync();
        await ReloadIssues(report);
    }

    private async Task<LoadingStatus> ReloadIssuesWhileFromCache(Report? report, CancellationToken ct = default)
    {
        var status = await ReloadIssues(report, ct);
        while (status == LoadingStatus.FromCache)
        {
            await Task.Delay(5000, ct);
            status = await ReloadIssues(report, ct);
        }

        return status;
    }

    private async Task<LoadingStatus> ReloadIssues(Report? report, CancellationToken ct = default)
    {
        _loading.OnNext(LoadingStatus.InProgress);
        if (report == null)
        {
            _allIssues.OnNext([]);
            _loading.OnNext(LoadingStatus.Loaded);
            return LoadingStatus.Loaded;
        }

        try
        {
            var resp = await apiClient.IssuesAllAsync(report.Id, ct);
            var fileIssues = await IssuesToFileIssuesAsync(resp.Select(e => e.ToDomain()).ToList(), ct);
            await cacheService.SaveCacheAsync(report.Id, "issues", fileIssues, ct);
            _allIssues.OnNext(fileIssues);
            _loading.OnNext(LoadingStatus.Loaded);
            return LoadingStatus.Loaded;
        }
        catch (HttpRequestException e) when (e.HttpRequestError == HttpRequestError.ConnectionError ||
                                             e.HttpRequestError == HttpRequestError.NameResolutionError)
        {
            var cache = await cacheService.LoadCacheAsync<FileIssue[]>(report.Id, "issues", ct);
            _loading.OnNext(cache == null ? LoadingStatus.Failed : LoadingStatus.FromCache);
            if (cache != null)
            {
                _allIssues.OnNext(cache);
                _loading.OnNext(LoadingStatus.FromCache);
                return LoadingStatus.FromCache;
            }

            alertService.SendAlert(AlertType.Warning, $"Не удалось загрузить список ошибок: {e.Message}");
            _loading.OnNext(LoadingStatus.Failed);
            return LoadingStatus.Failed;
        }
        catch (Exception e)
        {
            _loading.OnNext(LoadingStatus.Failed);
            alertService.SendAlert(AlertType.Warning, $"Не удалось загрузить список ошибок: {e.Message}");
            return LoadingStatus.Failed;
        }
    }

    private async Task<IReadOnlyList<FileIssue>> IssuesToFileIssuesAsync(IReadOnlyCollection<Issue> issues,
        CancellationToken ct = default)
    {
        var project = await projectService.CurrentProject.FirstAsync();
        if (project == null)
            return [];
        var provider = await projectService.GetFormatProviderAsync();
        return await provider.IssuesToFileIssuesAsync(project.Path, issues, ct);
    }

    public void SelectIssue(FileIssue? issue)
    {
        _selectedIssue.OnNext(issue);
    }

    public IReadOnlyList<FileIssue> UpdateIssuePositions(IEnumerable<FileIssue> source, string oldText, string newText)
    {
        var issues = source.OrderBy(e => e.Position?.Line ?? int.MaxValue).ToList();
        var result = new List<FileIssue>();
        var differ = new InlineDiffBuilder();
        var diff = differ.BuildDiffModel(oldText, newText);
        var oldIndex = 0;
        var newIndex = 0;
        foreach (var diffLine in diff.Lines)
        {
            if (diffLine.Type != ChangeType.Inserted)
                oldIndex++;
            if (diffLine.Type != ChangeType.Deleted)
            {
                newIndex++;
                while (issues.Count > 0 && issues[0].Position?.Line == oldIndex)
                {
                    result.Add(new FileIssue(issues[0].Issue, new FilePosition
                    {
                        Path = issues[0].Position?.Path ?? "???",
                        Line = newIndex
                    }));
                    issues.RemoveAt(0);
                }
            }
        }

        result.AddRange(issues.Select(issue => issue with { Position = null }));
        return result;
    }

    public async Task MarkRead(Issue issue, CancellationToken ct = default)
    {
        var report = await reportService.CurrentReport.FirstAsync();
        if (report == null)
            return;
        try
        {
            await apiClient.ReadAsync(report.Id, issue.Id,
                new MarkReadSchema
                {
                    IsRead = true,
                    CommentIds = issue.Comments.Where(e => e.IsRead == false).Select(e => e.Id).ToList()
                },
                ct);
            await ReloadIssues(report, ct);
        }
        catch (Exception e)
        {
            alertService.SendAlert(AlertType.Warning, $"Не удалось пометить комментарии как прочитанные: {e.Message}");
        }
    }
}