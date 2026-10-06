using ReportChecker.Abstractions;

namespace ReportChecker.Application.Services;

/// <summary>
/// Архив теста бенчмарка: читает файлы из каталога теста. Запись не поддерживается.
/// </summary>
public class BenchmarkDirectoryArchive(string directory, string? entryFile) : IFileArchive
{
    public string? Name => Path.GetFileName(entryFile);

    public string? EntryFilePath => entryFile;

    public Task<Stream?> ReadAsync()
    {
        return entryFile == null ? Task.FromResult<Stream?>(null) : ReadAsync(entryFile);
    }

    public Task<Stream?> ReadAsync(string name)
    {
        var fullPath = Resolve(name);
        if (fullPath == null || !File.Exists(fullPath))
            return Task.FromResult<Stream?>(null);
        return Task.FromResult<Stream?>(new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read));
    }

    /// <summary>
    /// Сопоставляет имя файла из документа с файлом внутри каталога теста,
    /// не позволяя выйти за его пределы.
    /// </summary>
    private string? Resolve(string name)
    {
        var normalized = name.Replace('\\', '/').TrimStart('/');
        if (normalized.Length == 0)
            return null;

        var root = Path.GetFullPath(directory);
        var candidate = Path.GetFullPath(Path.Combine(root, normalized.Replace('/', Path.DirectorySeparatorChar)));
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;

        return candidate.StartsWith(rootWithSeparator, StringComparison.Ordinal) ? candidate : null;
    }
}
