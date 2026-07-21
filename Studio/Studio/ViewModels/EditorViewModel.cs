using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AvaluxUI.Utils;
using ReactiveUI;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.ViewModels;

public class EditorViewModel(
    ILanguageService languageService,
    IFileService fileService,
    IIssueService issueService) : ViewModelBase
{
    public IReadOnlyList<EditorTabViewModel> TabViewModels
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = [];

    private readonly Dictionary<string, EditorFileViewModel> _fileViewModels = [];

    public IObservable<EditorFileViewModel?> FileViewModel => fileService.CurrentFile
        .Select(p => p == null ? null : _fileViewModels.GetValueOrDefault(p.Path));

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
        foreach (var path in files.Select(f => f.Path).Except(_fileViewModels.Keys).ToList())
        {
            var file = files.First(e => e.Path == path);
            _fileViewModels.Add(path, new EditorFileViewModel(file, languageService, fileService, issueService));
        }
        TabViewModels = files
            .Select(e => new EditorTabViewModel(e.Path, fileService, _fileViewModels[e.Path]))
            .ToList();
    }
}