using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using ReactiveUI;

namespace ReportChecker.Studio.ViewModels;

public abstract class ViewModelBase : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new();

    protected ViewModelBase()
    {
        this.WhenActivated(OnActivate);
    }

    protected virtual void OnActivate(CompositeDisposable disposable)
    {
        _OnActivateAsync(disposable);
    }

    private async void _OnActivateAsync(CompositeDisposable disposable)
    {
        try
        {
            await OnActivateAsync(disposable);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    protected virtual Task OnActivateAsync(CompositeDisposable disposable)
    {
        return Task.CompletedTask;
    }
}