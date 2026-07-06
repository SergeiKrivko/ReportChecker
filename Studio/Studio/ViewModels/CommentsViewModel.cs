using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using ReportChecker.Shared.Models;
using ReportChecker.Studio.Abstractions;

namespace ReportChecker.Studio.ViewModels;

public class CommentsViewModel(IIssueService issueService) : ViewModelBase
{
    public Issue? SelectedIssue
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    protected override Task OnActivateAsync(CompositeDisposable disposable)
    {
        issueService.SelectedIssue
            .Subscribe(e => SelectedIssue = e)
            .DisposeWith(disposable);

        return base.OnActivateAsync(disposable);
    }

    public void DeselectIssue()
    {
        issueService.SelectIssue(null);
    }
}