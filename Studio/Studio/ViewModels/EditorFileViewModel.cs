using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using AvaloniaEdit.Document;
using ReactiveUI;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.ViewModels;

public class EditorFileViewModel(string path, ILanguageService languageService) : ViewModelBase
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

    public LanguageCompletions GetCompletions(string triggerText, string fileText, int offset)
    {
        return languageService.GetCompletions(triggerText, fileText, offset);
    }
}