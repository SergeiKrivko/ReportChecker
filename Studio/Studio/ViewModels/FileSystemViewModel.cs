using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.ViewModels;

public class FileSystemViewModel(IProjectService projectService, EditorViewModel editorViewModel) : ViewModelBase
{
    public IObservable<IReadOnlyList<IProjectFileSystemEntry>?> Files { get; } = projectService.CurrentProject
        .Select(project => project == null ? null : LoadDirectory(Path.GetDirectoryName(project.Path)).Children.ToList());

    private static ProjectDirectory LoadDirectory(string? path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var directories = Directory.EnumerateDirectories(path)
            .Select(LoadDirectory)
            .ToList();
        var files = Directory.EnumerateFiles(path)
            .Select(file => new ProjectFile
            {
                Path = file,
                Name = Path.GetFileName(file)
            })
            .ToList();
        return new ProjectDirectory
        {
            Path = path,
            Name = Path.GetFileName(path),
            SubDirectories = directories,
            Files = files,
        };
    }

    public void OpenFile(string path)
    {
        editorViewModel.OpenFile(path);
    }
}