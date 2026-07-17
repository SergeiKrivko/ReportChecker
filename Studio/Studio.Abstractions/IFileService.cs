using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.Abstractions;

public interface IFileService
{
    public IObservable<IReadOnlyList<OpenedFile>> OpenedFiles { get; }
    public IObservable<OpenedFile?> CurrentFile { get; }

    public Task OpenFile(string path);
    public Task SelectFile(string? path);
    public Task CloseFile(string? path);
    public IObservable<object?> Load();
}