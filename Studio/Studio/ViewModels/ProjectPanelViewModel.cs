using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Threading.Tasks;
using ReactiveUI;
using ReportChecker.Shared.Models;
using ReportChecker.Studio.Abstractions;

namespace ReportChecker.Studio.ViewModels;

public class ProjectPanelViewModel(IReportService reportService) : ViewModelBase
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

        reportService.CurrentReport
            .Subscribe(e =>
            {
                CurrentReport = e;
                HasReport = e == null;
            })
            .DisposeWith(disposable);
    }
}