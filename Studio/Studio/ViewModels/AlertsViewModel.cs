using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.ViewModels;

public class AlertsViewModel(IAlertService alertService) : ViewModelBase
{
    public ObservableCollection<Alert> Alerts => alertService.Alerts;

    protected override void OnActivate(CompositeDisposable disposable)
    {
        base.OnActivate(disposable);

        alertService.Initialize()
            .Subscribe()
            .DisposeWith(disposable);
    }
}