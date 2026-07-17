using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using AvaluxUI.Utils;
using ReportChecker.Shared.Models;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Models;
using ReportChecker.Studio.Services.Settings;

namespace ReportChecker.Studio.Services;

public class FileService(IProjectService projectService, ISettingsSection settings) : IFileService
{
    private readonly BehaviorSubject<IReadOnlyList<OpenedFile>> _openedFiles = new([]);
    public IObservable<IReadOnlyList<OpenedFile>> OpenedFiles => _openedFiles;
    private readonly BehaviorSubject<OpenedFile?> _currentFile = new(null);
    public IObservable<OpenedFile?> CurrentFile => _currentFile;
    private readonly BehaviorSubject<FileJump> _fileJumps = new(new FileJump(""){IsHandled = true});
    public IObservable<FileJump> FileJumps => _fileJumps.Where(e => !e.IsHandled);

    public Task OpenFile(string path) => OpenFile(path, Guid.NewGuid());

    private async Task OpenFile(string path, Guid id)
    {
        var oldFiles = await _openedFiles.FirstAsync();
        var newFiles = oldFiles.Concat<OpenedFile>([
            new OpenedFile
            {
                Id = id,
                Path = path,
            }
        ]).ToList();
        _openedFiles.OnNext(newFiles);
        await SelectFile(path);
    }

    public async Task SelectFile(string? path)
    {
        var files = await OpenedFiles.FirstAsync();
        var file = files.FirstOrDefault(e => e.Path == path);
        _currentFile.OnNext(file);
    }

    public IObservable<object?> Load()
    {
        var obs0 = projectService.CurrentProject
            .Select(p => LoadProject(p).ToObservable())
            .Switch();
        var obs1 = OpenedFiles
            .WithLatestFrom(projectService.CurrentProject)
            .Select(a => SaveFilesList(a.Second, a.First).ToObservable())
            .Switch();
        var obs2 = CurrentFile
            .WithLatestFrom(projectService.CurrentProject)
            .Select(a => SaveCurrentFile(a.Second, a.First?.Path).ToObservable())
            .Switch();
        return obs0.Merge(obs1).Merge(obs2).SelectMany<System.Reactive.Unit, object?>(_ => Observable.Never<object?>());
    }

    private async Task LoadProject(Project? project)
    {
        if (project == null)
            return;
        var section = await settings.GetSection(project.Id.ToString());
        var activeFiles = await section.Get<OpenedFileSettings[]>("activeFiles", []);
        _openedFiles.OnNext(activeFiles.Select(f => new OpenedFile
        {
            Id = f.Id,
            Path = f.Path
        }).ToList());
        var currentFilePath = await section.Get<string>("currentFile");
        var currentFile = activeFiles.FirstOrDefault(e => e.Path == currentFilePath);
        _currentFile.OnNext(currentFile == null ? null : new OpenedFile
        {
            Id = currentFile.Id,
            Path = currentFile.Path
        });
    }

    private async Task SaveFilesList(Project? project, IEnumerable<OpenedFile> files)
    {
        if (project == null)
            return;
        var section = await settings.GetSection(project.Id.ToString());
        await section.Set("activeFiles", files.Select(e => new OpenedFileSettings
        {
            Id = e.Id,
            Path = e.Path,
        }));
    }

    private async Task SaveCurrentFile(Project? project, string? path)
    {
        if (project == null)
            return;
        var section = await settings.GetSection(project.Id.ToString());
        await section.Set("currentFile", path);
    }

    public async Task CloseFile(string? path)
    {
        var oldFiles = await OpenedFiles.FirstAsync();
        var newFiles = oldFiles.Where(e => e.Path != path).ToList();
        _openedFiles.OnNext(newFiles);
        if ((await CurrentFile.FirstAsync())?.Path == path)
        {
            await SelectFile(newFiles.Count > 0 ? newFiles[0].Path : null);
        }
    }

    public async Task JumpToFile(string path, int line)
    {
        await OpenFile(path);
        _fileJumps.OnNext(new FileJump(path, line));
    }

    public async Task JumpToFile(FilePosition position)
    {
        await OpenFile(position.Path);
        _fileJumps.OnNext(new FileJump(position.Path, position.Line));
    }
}