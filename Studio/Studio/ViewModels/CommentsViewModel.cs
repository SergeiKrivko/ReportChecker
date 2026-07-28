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

public class CommentsViewModel(
    ICommentsService commentsService,
    IIssueService issueService,
    IProjectService projectService,
    IFileService fileService,
    IWebLinksService webLinksService,
    IReportService reportService) : ViewModelBase
{
    public FileIssue? SelectedIssue
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool IsOpened
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = true;

    public IReadOnlyList<CommentViewModel> CommentViewModels
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = [];

    public string? CommentContent
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    protected override Task OnActivateAsync(CompositeDisposable disposable)
    {
        commentsService.Load().Subscribe().DisposeWith(disposable);
        issueService.SelectedIssue
            .Subscribe(e =>
            {
                SelectedIssue = e;
                IsOpened = e?.Issue.Status == IssueStatus.Open;
            })
            .DisposeWith(disposable);
        commentsService.AllComments
            .Do(e =>
            {
                CommentViewModels = e
                    .Select((c, i) => new CommentViewModel(c, projectService, fileService, commentsService, issueService)
                        { IsFirstComment = i == 0 })
                    .ToList();
                IsOpened = e.LastOrDefault(c => c.Status != null)?.Status == IssueStatus.Open;
            })
            .Subscribe()
            .DisposeWith(disposable);

        return base.OnActivateAsync(disposable);
    }

    public void DeselectIssue()
    {
        issueService.SelectIssue(null);
    }

    public async Task CloseIssue()
    {
        await commentsService.CreateComment(IssueStatus.Closed);
    }

    public async Task MarkIssueAsFixed()
    {
        await commentsService.CreateComment(IssueStatus.Fixed);
    }

    public async Task ReopenIssue()
    {
        await commentsService.CreateComment(IssueStatus.Open);
    }

    public async Task SendComment()
    {
        if (string.IsNullOrWhiteSpace(CommentContent))
            return;
        await commentsService.CreateComment(CommentContent);
    }

    public async Task GoToCode()
    {
        if (SelectedIssue == null || SelectedIssue.Position == null)
            return;
        await fileService.JumpToFile(SelectedIssue.Position.Value);
    }

    public async Task OpenInBrowser()
    {
        var report = await reportService.CurrentReport.FirstAsync();
        if (report == null || SelectedIssue == null)
            return;
        webLinksService.GoToIssue(report.Id, SelectedIssue.Issue.Id);
    }
}