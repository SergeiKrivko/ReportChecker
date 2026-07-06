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

    public CommentsViewModel CommentsViewModel => commentsViewModel;

    protected override async Task OnActivateAsync(CompositeDisposable disposable)
    {
        issueService.AllIssues
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

    private void UpdateIssues(IReadOnlyList<Issue> issues)
    {
        IssueViewModels = issues.Select(issue =>
        {
            if (_issueViewModels.TryGetValue(issue.Id, out var viewModel))
                return viewModel;
            viewModel = new IssueViewModel(issue, issueService);
            _issueViewModels[issue.Id] = viewModel;
            return viewModel;
        }).ToList();
    }
}