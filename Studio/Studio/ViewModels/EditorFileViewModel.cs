using System;
using System.IO;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using AvaloniaEdit.Document;
using ReactiveUI;

namespace ReportChecker.Studio.ViewModels;

public class EditorFileViewModel(string path) : ViewModelBase
{
    public string? Source
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    public TextDocument? Document
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string Path => path;

    private bool _isInitialized;

    protected override async Task OnActivateAsync(CompositeDisposable disposable)
    {
        if (_isInitialized)
            return;
        _isInitialized = true;
        Source = await File.ReadAllTextAsync(path);
        Document = new TextDocument(Source);
    }
}