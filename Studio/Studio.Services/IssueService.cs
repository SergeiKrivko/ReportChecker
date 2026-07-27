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

public class IssueService(IReportService reportService, IApiClient apiClient, IProjectService projectService)
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
            .Select<Report?, Task<int>>(report => ReloadIssues(report))
            .Switch()
            .Select<int, object>(e => e);
    }

    public async Task ReloadIssues()
    {
        var report = await reportService.CurrentReport.FirstAsync();
        await ReloadIssues(report);
    }

    private async Task<int> ReloadIssues(Report? report, CancellationToken ct = default)
    {
        _loading.OnNext(LoadingStatus.InProgress);
        if (report == null)
        {
            _allIssues.OnNext([]);
            _loading.OnNext(LoadingStatus.Loaded);
            return 0;
        }

        try
        {
            var resp = await apiClient.IssuesAllAsync(report.Id, ct);
            var fileIssues = await IssuesToFileIssuesAsync(resp.Select(e => e.ToDomain()).ToList(), ct);
            _allIssues.OnNext(fileIssues);
            _loading.OnNext(LoadingStatus.Loaded);
            return fileIssues.Count;
        }
        catch (HttpRequestException e)
        {
            _loading.OnNext(LoadingStatus.FromCache);
            return 0;
        }
        catch (Exception)
        {
            _loading.OnNext(LoadingStatus.Failed);
            return 0;
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
}