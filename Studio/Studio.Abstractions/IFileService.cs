using ReportChecker.Shared.Models;
using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.Abstractions;

public interface IFileService
{
    public IObservable<IReadOnlyList<OpenedFile>> OpenedFiles { get; }
    public IObservable<OpenedFile?> CurrentFile { get; }
    public IObservable<FileJump> FileJumps { get; }
    public IObservable<FilePatch> FilePatches { get; }

    public Task OpenFile(string path);
    public Task SelectFile(string? path);
    public Task CloseFile(string? path);
    public IObservable<object?> Load();
    public Task JumpToFile(string path, int line);
    public Task JumpToFile(FilePosition position);
    public Task ApplyPatch(FilePatch patch);
}