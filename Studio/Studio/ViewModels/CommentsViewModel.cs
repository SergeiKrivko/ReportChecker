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
using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.ViewModels;

public class CommentsViewModel(
    ICommentsService commentsService,
    IIssueService issueService,
    IProjectService projectService,
    IFileService fileService,
    IWebLinksService webLinksService,
    IReportService reportService,
    IAlertService alertService) : ViewModelBase
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

    protected override async Task OnActivateAsync(CompositeDisposable disposable)
    {
        await base.OnActivateAsync(disposable);

        commentsService.Load().Subscribe().DisposeWith(disposable);
        issueService.SelectedIssue
            .Do(e =>
            {
                SelectedIssue = e;
                IsOpened = e?.Issue.Status == IssueStatus.Open;
            })
            .Select(async issue =>
            {
                if (issue != null)
                    await issueService.MarkRead(issue.Issue.Id);
                return 0;
            })
            .Subscribe()
            .DisposeWith(disposable);
        commentsService.AllComments
            .Do(e =>
            {
                CommentViewModels = e
                    .Select((c, i) =>
                        new CommentViewModel(c, projectService, fileService, commentsService, issueService,
                                alertService)
                            { IsFirstComment = i == 0 })
                    .ToList();
                IsOpened = e.LastOrDefault(c => c.Status != null)?.Status == IssueStatus.Open;
            })
            .Subscribe()
            .DisposeWith(disposable);
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
        try
        {
            await commentsService.CreateComment(CommentContent);
        }
        catch (Exception e)
        {
            alertService.SendAlert(AlertType.Error, $"Не удалось отправить комментарий: {e.Message}");
        }
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