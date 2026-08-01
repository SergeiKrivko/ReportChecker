using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.ViewModels;

public class MainWindowViewModel(
    ProjectSelectorViewModel projectSelectorViewModel,
    EditorViewModel editorViewModel,
    RightPanelViewModel rightPanelViewModel,
    AuthButtonViewModel authButtonViewModel,
    ReportPanelViewModel reportPanelViewModel,
    IIssueService issueService,
    ViewModels.AlertsViewModel alertsViewModel,
    IAlertService alertService,
    BuildPanelViewModel buildPanelViewModel) : ViewModelBase
{
    public ProjectSelectorViewModel ProjectSelectorViewModel => projectSelectorViewModel;
    public AuthButtonViewModel AuthButtonViewModel => authButtonViewModel;
    public ReportPanelViewModel ReportPanelViewModel => reportPanelViewModel;
    public RightPanelViewModel RightPanelViewModel => rightPanelViewModel;
    public EditorViewModel EditorViewModel => editorViewModel;
    public AlertsViewModel AlertsViewModel => alertsViewModel;
    public BuildPanelViewModel BuildPanelViewModel => buildPanelViewModel;

    protected override void OnActivate(CompositeDisposable disposable)
    {
        base.OnActivate(disposable);

        issueService.Load()
            .Subscribe()
            .DisposeWith(disposable);
    }
}