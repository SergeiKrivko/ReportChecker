using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using ReactiveUI;

namespace ReportChecker.Studio.ViewModels;

public class EditorTabViewModel(string path, EditorViewModel editorViewModel) : ViewModelBase
{
    public string Name { get; } = System.IO.Path.GetFileName(path);

    public bool IsSelected
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    protected override void OnActivate(CompositeDisposable disposable)
    {
        editorViewModel.SelectedFile
            .Subscribe(f => IsSelected = f == path)
            .DisposeWith(disposable);
    }

    public void Close()
    {
        editorViewModel.CloseFile(path);
    }

    public void SelectFile()
    {
        editorViewModel.SelectFile(path);
    }
}