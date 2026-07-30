using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.ViewModels;

public class EditorViewModel(
    ILanguageService languageService,
    IFileService fileService,
    IIssueService issueService,
    IReportService reportService,
    IAlertService alertService,
    IProjectService projectService) : ViewModelBase
{
    public IReadOnlyList<EditorTabViewModel> TabViewModels
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = [];

    private readonly Dictionary<string, EditorFileViewModel> _fileViewModels = [];

    public IObservable<EditorFileViewModel?> FileViewModel => fileService.CurrentFile
        .Select(p => p == null ? null : GetEditorFileViewModel(p));

    protected override async Task OnActivateAsync(CompositeDisposable disposable)
    {
        await base.OnActivateAsync(disposable);

        fileService.Load()
            .Subscribe()
            .DisposeWith(disposable);
        fileService.OpenedFiles
            .Do(UpdateEditorTabs)
            .Subscribe()
            .DisposeWith(disposable);
    }

    public async Task OpenFile(string path) => await fileService.OpenFile(path);

    private void UpdateEditorTabs(IReadOnlyCollection<OpenedFile> files)
    {
        foreach (var path in _fileViewModels.Keys.Except(files.Select(f => f.Path)).ToList())
        {
            _fileViewModels.Remove(path);
        }

        TabViewModels = files
            .Select(e => new EditorTabViewModel(e.Path, fileService, GetEditorFileViewModel(e)))
            .ToList();
    }

    private EditorFileViewModel GetEditorFileViewModel(OpenedFile file)
    {
        if (_fileViewModels.TryGetValue(file.Path, out var res))
            return res;
        var vm = new EditorFileViewModel(file, languageService, fileService, issueService, reportService, alertService,
            projectService);
        _fileViewModels.Add(file.Path, vm);
        return vm;
    }
}