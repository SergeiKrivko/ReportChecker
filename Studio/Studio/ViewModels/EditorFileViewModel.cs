using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AvaloniaEdit.Document;
using ReactiveUI;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.ViewModels;

public class EditorFileViewModel(OpenedFile file, ILanguageService languageService)
    : ViewModelBase
{
    public Guid Id => file.Id;

    public TextDocument? Document
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool IsModified
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    private string? _text;

    public string Path => file.Path;

    private bool _isInitialized;

    protected override async Task OnActivateAsync(CompositeDisposable disposable)
    {
        if (_isInitialized)
            return;
        _isInitialized = true;
        await Load();

        Document.ObservableForProperty(e => e.Text)
            .Do(t =>
            {
                IsModified = true;
                _text = t.Value;
            })
            .Select(e => e.Value)
            .Sample(TimeSpan.FromSeconds(3))
            .Subscribe(_ => SaveBackup())
            .DisposeWith(disposable);
    }

    private string BackupPath => System.IO.Path.Join(Config.DataPath, "Backups", Id.ToString());

    private void SaveBackup()
    {
        if (Document == null || !IsModified)
            return;
        Directory.CreateDirectory(System.IO.Path.Join(Config.DataPath, "Backups"));
        File.WriteAllText(BackupPath, _text);
    }

    public void DeleteBackup()
    {
        if (File.Exists(BackupPath))
            File.Delete(BackupPath);
    }

    public async Task Save()
    {
        if (Document == null)
            return;
        await File.WriteAllTextAsync(Path, Document.Text);
        IsModified = false;
        DeleteBackup();
    }

    public LanguageCompletions GetCompletions(string triggerText, string fileText, int offset)
    {
        return languageService.GetCompletions(triggerText, fileText, offset);
    }

    private async Task Load()
    {
        if (File.Exists(BackupPath))
        {
            Document = new TextDocument(await File.ReadAllTextAsync(BackupPath));
            IsModified = true;
        }
        else
        {
            Document = new TextDocument(await File.ReadAllTextAsync(file.Path));
        }
    }
}