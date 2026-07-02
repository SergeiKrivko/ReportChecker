using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveUI;
using ReportChecker.Studio.Abstractions;

namespace ReportChecker.Studio.ViewModels;

public class EditorViewModel(ILanguageService languageService) : ViewModelBase
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

    public IObservable<string?> SelectedFile => _selectedFileSubject;
    private readonly Dictionary<string, EditorFileViewModel> _fileViewModels = [];

    public IObservable<EditorFileViewModel?> FileViewModel => SelectedFile
        .Select(p => p == null ? null : _fileViewModels.GetValueOrDefault(p));

    public void OpenFile(string path)
    {
        Files = Files.Concat([path]).ToList();
        UpdateEditorTabs();
        _fileViewModels[path] = new EditorFileViewModel(path, languageService);
        SelectFile(path);
    }

    public void CloseFile(string path)
    {
        Files = Files.Where(e => e != path).ToList();
        UpdateEditorTabs();
        _fileViewModels.Remove(path);
        if (_selectedFile == path)
        {
            SelectFile(Files.Count > 0 ? Files[0] : null);
        }
    }

    private void UpdateEditorTabs()
    {
        TabViewModels = Files
            .Select(e => new EditorTabViewModel(e, this))
            .ToList();
    }

    public void SelectFile(string? path)
    {
        // if (_selectedFile == path)
        //     return;
        _selectedFile = path;
        _selectedFileSubject.OnNext(path);
    }
}