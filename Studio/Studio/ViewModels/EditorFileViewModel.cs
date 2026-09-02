using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
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
    IIssueService issueService,
    IReportService reportService,
    IAlertService alertService,
    IProjectService projectService)
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

    private readonly BehaviorSubject<int> _lineCount = new(0);

    public IObservable<IReadOnlyList<FileIssue>> Issues => issueService.AllIssues
        .CombineLatest(_lineCount
            .DistinctUntilChanged()
            .Select(_ => Document?.Text ?? ""))
        .Select(c =>
            issueService.UpdateIssuePositions(c.First.Where(e => e.Position?.Path == file.Path), InitialText,
                c.Second));

    public IIssueService IssueService => issueService;

    private string? _text;

    public string Path => file.Path;
    public IObservable<FileJump> Jumps => fileService.FileJumps.Where(e => e.Path == Path);

    private bool _isInitialized;

    protected override async Task OnActivateAsync(CompositeDisposable disposable)
    {
        if (!_isInitialized)
        {
            _isInitialized = true;
            await Load();
        }

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
        Document.ObservableForProperty(e => e.Text)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Select(async e =>
            {
                await languageService.ParseFileAsync(Path, e.Value);
                return true;
            })
            .Switch()
            .Subscribe()
            .DisposeWith(disposable);
        fileService.FilePatches
            .Where(e => e.Path == Path)
            .Do(ApplyPatch)
            .Subscribe()
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
        await PushCheckAsync();
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

    private void ApplyPatch(FilePatch patch)
    {
        try
        {
            if (patch.Path != Path)
                return;

            var builder = new StringBuilder();
            var lines = Document?.Text?.Replace("\r\n", "\n").Split('\n') ?? [];
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var patchLines = patch.Lines.Where(e => e.Number == i + 1).ToList();
                if (patchLines.All(e => e.Type == PatchLineType.Add))
                    builder.AppendLine(line);
                else
                {
                    var singleLine = patchLines.SingleOrDefault(e => e.Type != PatchLineType.Add);
                    if (singleLine != null && singleLine.PreviousContent != line)
                        throw new Exception(
                            $"Conflict when trying to apply patch: '{singleLine.PreviousContent}' --- '{line}'");
                    if (singleLine?.Type == PatchLineType.Modify)
                        builder.AppendLine(singleLine.Content);
                }

                foreach (var l in patchLines.Where(e => e.Type == PatchLineType.Add))
                    builder.AppendLine(l.Content);
            }

            Document?.Text = builder.ToString();
            patch.IsHandled = true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private async Task PushCheckAsync()
    {
        var status = await reportService.Status.FirstAsync();
        if (status == ProgressStatus.InProgress)
        {
            alertService.SendAlert(AlertType.Warning, "Невозможно отправить новую версию, пока идет проверка предыдущей");
            return;
        }

        try
        {
            var pack = await projectService.PackCurrentProjectAsync();
            await reportService.CheckAsync(pack);
            alertService.SendAlert(AlertType.Success, "Новая версия отправлена на проверку");
        }
        catch (Exception e)
        {
            alertService.SendAlert(AlertType.Error, $"Не удалось отравить новую версию на проверку: {e}");
        }
    }
}