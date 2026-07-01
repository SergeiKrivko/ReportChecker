using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ReactiveUI;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.ViewModels;

public class ProjectSelectorViewModel(IProjectService projectService) : ViewModelBase
{
    public IObservable<Project?> CurrentProject => projectService.CurrentProject;

    public IObservable<bool> IsProjectSelected { get; } = projectService.CurrentProject
        .Select(e => e != null);

    public IReadOnlyList<Project> RecentProjects
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = [];

    public bool HasRecentProjects
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    protected override async Task OnActivateAsync(CompositeDisposable disposable)
    {
        await projectService.OpenLastProject();
        RecentProjects = await projectService.GetRecentProjects();
        HasRecentProjects = RecentProjects.Count > 0;
    }

    public async Task OpenProject()
    {
        if (Application.Current?.ApplicationLifetime is not ClassicDesktopStyleApplicationLifetime desktopLifetime)
            return;
        var files = await desktopLifetime.MainWindow!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("LaTeX file")
                {
                    Patterns = ["*.tex"]
                },
            ],
            Title = "Выберите файл отчета"
        });
        if (files.Count < 1)
            return;
        var path = files[0].Path.LocalPath;
        await projectService.OpenProject(path);
    }
}