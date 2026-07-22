using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using AvaloniaEdit.Document;
using ReactiveUI;
using ReportChecker.Shared.Models;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.ViewModels;

public class EditorFileViewModel(
    OpenedFile file,
    ILanguageService languageService,
    IFileService fileService,
    IIssueService issueService)
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
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    private string InitialText
    {
        get => field ??= File.ReadAllText(Path);
        set;
    }

    private readonly Subject<int> _lineCount = new();
    public IObservable<IReadOnlyList<FileIssue>> Issues => issueService.AllIssues
        .CombineLatest(_lineCount
            .DistinctUntilChanged()
            .Select(_ => Document?.Text ?? ""))
        .Select(c =>
            issueService.UpdateIssuePositions(c.First.Where(e => e.Position?.Path == file.Path), InitialText, c.Second));

    public IIssueService IssueService => issueService;

    private string? _text;

    public string Path => file.Path;
    public IObservable<FileJump> Jumps => fileService.FileJumps.Where(e => e.Path == Path);

    private bool _isInitialized;

    protected override async Task OnActivateAsync(CompositeDisposable disposable)
    {
        if (_isInitialized)
            return;
        _isInitialized = true;
        await Load();

        _lineCount.OnNext(Document?.LineCount ?? 0);
        Document.ObservableForProperty(e => e.Text)
            .Do(t =>
            {
                IsModified = true;
                _text = t.Value;
                _lineCount.OnNext(Document?.LineCount ?? 0);
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
        InitialText = Document.Text;
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