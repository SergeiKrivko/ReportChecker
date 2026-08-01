using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using AvaluxUI.Utils;
using ReportChecker.Shared.Models;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Models;
using ReportChecker.Studio.Services.Settings;

namespace ReportChecker.Studio.Services;

public class FileService(IProjectService projectService, ISettingsSection settings, IAlertService alertService)
    : IFileService
{
    private readonly BehaviorSubject<IReadOnlyList<OpenedFile>> _openedFiles = new([]);
    public IObservable<IReadOnlyList<OpenedFile>> OpenedFiles => _openedFiles;
    private readonly BehaviorSubject<OpenedFile?> _currentFile = new(null);
    public IObservable<OpenedFile?> CurrentFile => _currentFile;
    private readonly BehaviorSubject<FileJump> _fileJumps = new(new FileJump("") { IsHandled = true });
    public IObservable<FileJump> FileJumps => _fileJumps.Where(e => !e.IsHandled);
    private readonly BehaviorSubject<FilePatch> _filePatches = new(new FilePatch { Path = "", IsHandled = true });
    public IObservable<FilePatch> FilePatches => _filePatches.Where(e => !e.IsHandled);

    public Task OpenFile(string path) => OpenFile(path, Guid.NewGuid());

    private async Task OpenFile(string path, Guid id)
    {
        if (IsBinary(path))
        {
            OpenFileWithSystemApp(path);
            return;
        }
        var oldFiles = await _openedFiles.FirstAsync();
        if (oldFiles.All(e => e.Path != path))
        {
            var newFiles = oldFiles.Concat<OpenedFile>([
                new OpenedFile
                {
                    Id = id,
                    Path = path,
                }
            ]).ToList();
            _openedFiles.OnNext(newFiles);
        }

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
        _currentFile.OnNext(currentFile == null
            ? null
            : new OpenedFile
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

    public async Task ApplyPatch(FilePatch patch)
    {
        await OpenFile(patch.Path);
        _filePatches.OnNext(patch);
    }

    private static bool IsBinary(string filePath, int requiredConsecutiveNul = 1)
    {
        const int charsToCheck = 8000;
        const char nulChar = '\0';

        int nulCount = 0;

        using var streamReader = new StreamReader(filePath);

        for (var i = 0; i < charsToCheck; i++)
        {
            if (streamReader.EndOfStream)
                return false;

            if ((char)streamReader.Read() == nulChar)
            {
                nulCount++;

                if (nulCount >= requiredConsecutiveNul)
                    return true;
            }
            else
            {
                nulCount = 0;
            }
        }

        return false;
    }

    private void OpenFileWithSystemApp(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(path),
                Verb = "OPEN"
            });
        }
        else if (OperatingSystem.IsLinux())
        {
            Process.Start(new ProcessStartInfo { FileName = "xdg-open", Arguments = path, UseShellExecute = true });
        }
        else if (OperatingSystem.IsMacOS())
        {
            Process.Start(new ProcessStartInfo { FileName = "open", Arguments = path, UseShellExecute = true });
        }
        else
        {
            alertService.SendAlert(AlertType.Error, "Can not open file on this operating system");
        }
    }
}