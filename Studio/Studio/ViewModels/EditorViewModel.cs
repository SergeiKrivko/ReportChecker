using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using AvaluxUI.Utils;
using ReactiveUI;
using ReportChecker.Studio.Abstractions;

namespace ReportChecker.Studio.ViewModels;

public class EditorViewModel(
    ILanguageService languageService,
    ISettingsSection settings,
    IProjectService projectService) : ViewModelBase
{
    public IReadOnlyList<string> Files
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = [];

    public IReadOnlyList<EditorTabViewModel> TabViewModels
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = [];

    private string? _selectedFile;
    private readonly Subject<string?> _selectedFileSubject = new();

    public IObservable<string?> SelectedFileObservable => _selectedFileSubject;

    public string? SelectedFile => _selectedFile;

    private readonly Dictionary<string, EditorFileViewModel> _fileViewModels = [];

    public IObservable<EditorFileViewModel?> FileViewModel => SelectedFileObservable
        .Select(p => p == null ? null : _fileViewModels.GetValueOrDefault(p));

    protected override async Task OnActivateAsync(CompositeDisposable disposable)
    {
        await base.OnActivateAsync(disposable);

        this.ObservableForProperty(e => e.Files)
            .Select(_ => Observable.FromAsync(SaveFilesList))
            .Concat()
            .Subscribe()
            .DisposeWith(disposable);

        await LoadFilesList();
    }

    public void OpenFile(string path) => OpenFile(path, Guid.NewGuid());

    private void OpenFile(string path, Guid id)
    {
        if (!_fileViewModels.ContainsKey(path))
        {
            _fileViewModels[path] = new EditorFileViewModel(path, id, languageService);
            Files = Files.Concat([path]).ToList();
            UpdateEditorTabs();
        }

        SelectFile(path);
    }

    public void CloseFile(string path)
    {
        _fileViewModels.Remove(path);
        Files = Files.Where(e => e != path).ToList();
        UpdateEditorTabs();
        if (_selectedFile == path)
        {
            SelectFile(Files.Count > 0 ? Files[0] : null);
        }
    }

    private void UpdateEditorTabs()
    {
        TabViewModels = Files
            .Select(e => new EditorTabViewModel(e, this, _fileViewModels[e]))
            .ToList();
    }

    public void SelectFile(string? path)
    {
        // if (_selectedFile == path)
        //     return;
        _selectedFile = path;
        _selectedFileSubject.OnNext(path);
    }

    private async Task SaveFilesList()
    {
        var project = await projectService.CurrentProject.FirstAsync();
        if (project == null)
            return;
        var section = await settings.GetSection(project.Id.ToString());
        await section.Set("activeFiles", Files.Select(e => new ActiveFileSettings
        {
            Id = _fileViewModels[e].Id,
            Path = e,
        }));
    }

    private async Task LoadFilesList()
    {
        var project = await projectService.CurrentProject.FirstAsync();
        if (project == null)
            return;
        var section = await settings.GetSection(project.Id.ToString());
        var files = await section.Get<ActiveFileSettings[]>("activeFiles", []);
        foreach (var file in files)
        {
            OpenFile(file.Path, file.Id);
        }
    }

    private class ActiveFileSettings
    {
        public required Guid Id { get; init; }
        public required string Path { get; init; }
    }
}