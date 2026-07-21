using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using ReportChecker.Shared.Models;
using ReportChecker.Studio.Abstractions;

namespace ReportChecker.Studio.ViewModels;

public class IssueListViewModel(
    IReportService reportService,
    IIssueService issueService,
    CommentsViewModel commentsViewModel) : ViewModelBase
{
    public IObservable<Report?> CurrentReport => reportService.CurrentReport;

    private IReadOnlyList<FileIssue> _issues = [];
    private readonly Dictionary<Guid, IssueViewModel> _issueViewModels = [];

    public IReadOnlyList<IssueViewModel> IssueViewModels
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = [];

    public bool HasSelectedIssue
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool IsActiveIssues
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = true;

    public bool IsClosedIssues
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool IsFixedIssues
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public CommentsViewModel CommentsViewModel => commentsViewModel;

    protected override async Task OnActivateAsync(CompositeDisposable disposable)
    {
        issueService.AllIssues
            .Do(e => _issues = e)
            .Subscribe(UpdateIssues)
            .DisposeWith(disposable);
        issueService.SelectedIssue
            .Subscribe(issue => HasSelectedIssue = issue != null)
            .DisposeWith(disposable);
        issueService.Load()
            .Subscribe()
            .DisposeWith(disposable);
        await reportService.GetAllReports();
    }

    private void UpdateIssues(IReadOnlyList<FileIssue> issues)
    {
        var status = IsActiveIssues ? IssueStatus.Open : IsClosedIssues ? IssueStatus.Closed : IssueStatus.Fixed;
        IssueViewModels = issues
            .Where(issue => issue.Issue.Status == status)
            .OrderBy(issue => issue.Issue.Priority)
            .Select(issue =>
        {
            if (_issueViewModels.TryGetValue(issue.Issue.Id, out var viewModel))
                return viewModel;
            viewModel = new IssueViewModel(issue, issueService);
            _issueViewModels[issue.Issue.Id] = viewModel;
            return viewModel;
        }).ToList();
    }

    public void ShowActiveIssues()
    {
        IsActiveIssues = true;
        IsClosedIssues = false;
        IsFixedIssues = false;
        UpdateIssues(_issues);
    }

    public void ShowClosedIssues()
    {
        IsActiveIssues = false;
        IsClosedIssues = true;
        IsFixedIssues = false;
        UpdateIssues(_issues);
    }

    public void ShowFixedIssues()
    {
        IsActiveIssues = false;
        IsClosedIssues = false;
        IsFixedIssues = true;
        UpdateIssues(_issues);
    }

    public async Task ReloadIssues()
    {
        await issueService.ReloadIssues();
    }
}