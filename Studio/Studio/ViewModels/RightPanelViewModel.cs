using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using ReactiveUI;
using ReportChecker.Studio.Abstractions;

namespace ReportChecker.Studio.ViewModels;

public class RightPanelViewModel(
    FileSystemViewModel fileSystemViewModel,
    IssueListViewModel issueListViewModel,
    IIssueService issueService) : ViewModelBase
{
    public bool IsFileSystemActive
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool IsIssuesActive
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ViewModelBase? ActiveViewModel
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool IsOpen
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public double PanelWidth
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = 350;

    public void ActivateFileSystem()
    {
        ActiveViewModel = IsFileSystemActive ? fileSystemViewModel : null;
        IsOpen = ActiveViewModel != null;
        IsIssuesActive = false;
    }

    public void ActivateIssues()
    {
        ActiveViewModel = IsIssuesActive ? issueListViewModel : null;
        IsOpen = ActiveViewModel != null;
        IsFileSystemActive = false;
    }

    protected override void OnActivate(CompositeDisposable disposable)
    {
        base.OnActivate(disposable);

        issueService.SelectedIssue
            .WhereNotNull()
            .Subscribe(_ =>
            {
                IsIssuesActive = true;
                ActivateIssues();
            })
            .DisposeWith(disposable);
    }
}