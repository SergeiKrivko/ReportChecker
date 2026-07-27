using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AvaluxUI.Controls;
using ReactiveUI;
using ReportChecker.Shared.Models;
using ReportChecker.Studio.Abstractions;

namespace ReportChecker.Studio.ViewModels;

public class ReportPanelViewModel(
    IReportService reportService,
    IProjectService projectService,
    IWebLinksService webLinksService,
    IIssueService issueService) : ViewModelBase
{
    public Report? CurrentReport
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool IsProgress
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool HasReport
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public int CriticalIssuesCount
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public int MediumIssuesCount
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public int LowIssuesCount
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    protected override async Task OnActivateAsync(CompositeDisposable disposable)
    {
        await base.OnActivateAsync(disposable);

        await reportService.GetAllReports();
        reportService.CurrentReport
            .Subscribe(e =>
            {
                CurrentReport = e;
                HasReport = e != null;
                CriticalIssuesCount = CountIssues(e, 1, 2);
                MediumIssuesCount = CountIssues(e, 3, 5);
                LowIssuesCount = CountIssues(e, 5, 10);
            })
            .DisposeWith(disposable);
        issueService.AllIssues
            .Subscribe(e =>
            {
                CriticalIssuesCount = CountIssues(e, 1, 2);
                MediumIssuesCount = CountIssues(e, 3, 5);
                LowIssuesCount = CountIssues(e, 5, 10);
            })
            .DisposeWith(disposable);
        reportService.Status
            .Subscribe(e => { IsProgress = e == ProgressStatus.InProgress; })
            .DisposeWith(disposable);
    }

    private static int CountIssues(Report? report, int minPriority, int maxPriority)
    {
        if (report == null)
            return 0;
        var result = 0;
        for (var i = minPriority; i <= maxPriority; i++)
            result += report.IssueCount.GetValueOrDefault(i.ToString(), 0);
        return result;
    }

    private static int CountIssues(IReadOnlyList<FileIssue> issues, int minPriority, int maxPriority)
    {
        return issues
            .Count(e => e.Issue.Priority >= minPriority && e.Issue.Priority <= maxPriority);
    }

    public async Task PushReportAsync()
    {
        if (!await PromptDialog.Prompt("Ваши файлы будут загружены на сервер для поиска ошибок. Продолжить?"))
            return;
        try
        {
            var project = await projectService.CurrentProject.FirstAsync() ??
                          throw new Exception("Project not selected");
            var pack = await projectService.PackCurrentProjectAsync();
            var reportId = await reportService.CreateAsync(pack, project.Name ?? Guid.NewGuid().ToString());
            await projectService.SetReportId(project.Id, reportId);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public void OpenReportSettings()
    {
        if (CurrentReport != null)
            webLinksService.GoToReportSettings(CurrentReport.Id);
    }

    public void OpenWebReport()
    {
        if (CurrentReport != null)
            webLinksService.GoToReport(CurrentReport.Id);
    }
}