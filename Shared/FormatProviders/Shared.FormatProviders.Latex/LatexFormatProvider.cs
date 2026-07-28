using System.IO.Compression;
using System.Text;
using ReportChecker.Shared.Models;
using IFormatProvider = ReportChecker.Shared.Abstractions.IFormatProvider;

namespace ReportChecker.Shared.FormatProviders.Latex;

public class LatexFormatProvider : IFormatProvider
{
    public string Key => "Latex";

    public Task<bool> TestSourceAsync(string path)
    {
        return Task.FromResult(path.EndsWith(".tex"));
    }

    public async Task<SourcePack> PackSourcesAsync(string path)
    {
        var memoryStream = new MemoryStream();
        var rootPath =
            Path.GetFullPath(Path.GetDirectoryName(path) ?? ".");
        using (var zip = await ZipArchive.CreateAsync(memoryStream, ZipArchiveMode.Create, true, Encoding.UTF8))
        {
            foreach (var file in Directory.EnumerateFiles(rootPath, "*.tex", SearchOption.AllDirectories))
            {
                var entryPath = Path.GetRelativePath(rootPath, file);
                await zip.CreateEntryFromFileAsync(file, entryPath, CompressionLevel.Optimal);
            }
        }

        memoryStream.Seek(0, SeekOrigin.Begin);
        return new SourcePack(Key, memoryStream, Path.GetFileName(Path.ChangeExtension(path, ".zip")),
            Path.GetFileName(path));
    }

    public Task<DateTime> GetUpdateTimeAsync(string path)
    {
        path = Path.GetDirectoryName(path) ?? ".";
        var time = Directory.EnumerateFiles(path, "*.tex", SearchOption.AllDirectories)
            .Select(File.GetLastWriteTimeUtc)
            .Max();
        return Task.FromResult(time);
    }

    private static int LineLevel(LatexCommand command, out string title)
    {
        title = command.Argument ?? "";
        return command.Command switch
        {
            "chapter" => 0,
            "section" => 1,
            "subsection" => 2,
            "subsubsection" => 3,
            _ => int.MaxValue,
        };
    }

    private const string ChapterSeparator = "//";

    public async Task ApplyPatchAsync(string path, string chapter, IEnumerable<PatchLine> lines,
        CancellationToken ct = default)
    {
        await _ApplyPatchAsync(path, chapter, lines, ct);
    }

    private static async Task<bool> _ApplyPatchAsync(string filePath, string chapter, IEnumerable<PatchLine> lines,
        CancellationToken ct = default)
    {
        var fileName = Path.GetFileName(filePath);
        var directoryName = Path.GetDirectoryName(filePath) ?? ".";
        lines = lines.ToList();
        var text = await File.ReadAllTextAsync(filePath, ct);

        var lst = new List<string>();
        using (var reader = new StringReader(text))
        {
            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                lst.Add(line);
            }
        }

        var patchApplied = false;
        var path = new List<string> { fileName.TrimStart('/') };
        var isPatchChapter = chapter == string.Join(ChapterSeparator, path.Where(e => !string.IsNullOrWhiteSpace(e)));
        var lineNumber = 0;
        var includedFiles = new List<string>();
        var builder = new StringBuilder();
        foreach (var line in lst)
        {
            if (line.TryParseCommand(out var command))
            {
                var level = LineLevel(command, out var title);
                if (level <= 3)
                {
                    while (level < path.Count)
                        path.RemoveAt(path.Count - 1);
                    while (level > path.Count)
                        path.Add("");
                    path.Add(title);
                    isPatchChapter =
                        chapter == string.Join(ChapterSeparator, path.Where(e => !string.IsNullOrWhiteSpace(e)));
                }
                else if (command is { Command: "include", Argument: not null })
                {
                    includedFiles.Add(command.Argument);
                }
            }

            if (isPatchChapter)
            {
                lineNumber++;
                var currentLines = lines.Where(e => e.Number == lineNumber).ToList();
                if (currentLines.All(e => e.Type == PatchLineType.Add))
                    builder.AppendLine(line);
                else if (currentLines.Any(e => e.Type == PatchLineType.Modify))
                {
                    var modifyLine = currentLines.Single(e => e.Type == PatchLineType.Modify);
                    builder.AppendLine(modifyLine.Content);
                }

                foreach (var addLine in currentLines.Where(e => e.Type == PatchLineType.Add))
                {
                    builder.AppendLine(addLine.Content);
                }

                patchApplied = true;
            }
            else
            {
                builder.AppendLine(line);
            }
        }

        if (patchApplied)
        {
            var newText = builder.ToString();
            var span = text.EndsWith('\n')
                ? newText.AsSpan()
                : newText.EndsWith("\r\n")
                    ? newText.AsSpan(0, newText.Length - 2)
                    : newText.AsSpan(0, newText.Length - 1);
            await File.WriteAllTextAsync(filePath, span.ToString(), ct);
        }

        return await includedFiles.ToAsyncEnumerable()
            .Select(async (f, _, t) =>
                await _ApplyPatchAsync($"{directoryName}/{f}.tex".TrimStart('/'), chapter, lines, t))
            .AnyAsync(ct);
    }

    public async Task<FilePatch?> PatchToFilePatchAsync(string filePath, string chapter, IEnumerable<PatchLine> patchLines, CancellationToken ct = default)
    {
        var fileName = Path.GetFileName(filePath);
        var directoryName = Path.GetDirectoryName(filePath) ?? ".";
        var text = await File.ReadAllTextAsync(filePath, ct);

        var lst = new List<string>();
        using (var reader = new StringReader(text))
        {
            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                lst.Add(line);
            }
        }

        var path = new List<string> { fileName.TrimStart('/') };
        var lineNumber = 0;
        var includedFiles = new List<string>();
        foreach (var line in lst)
        {
            if (line.TryParseCommand(out var command))
            {
                var level = LineLevel(command, out var title);
                if (level <= 3)
                {
                    while (level < path.Count)
                        path.RemoveAt(path.Count - 1);
                    while (level > path.Count)
                        path.Add("");
                    path.Add(title);
                    if (chapter == string.Join(ChapterSeparator, path.Where(e => !string.IsNullOrWhiteSpace(e))))
                    {
                        return new FilePatch
                        {
                            Path = filePath,
                            Lines = patchLines.Select(e => new PatchLine
                            {
                                Number = e.Number + lineNumber,
                                Content = e.Content,
                                PreviousContent = e.PreviousContent,
                                Type = e.Type,
                            }).ToList()
                        };
                    }
                }
                else if (command is { Command: "include", Argument: not null })
                {
                    includedFiles.Add(command.Argument);
                }
            }
            lineNumber++;
        }

        return await includedFiles.ToAsyncEnumerable()
            .Select(async (f, _, t) =>
                await PatchToFilePatchAsync($"{directoryName}/{f}.tex".TrimStart('/'), chapter, patchLines, t))
            .FirstOrDefaultAsync(e => e != null,ct);
    }

    public async Task<FilePosition?> FilePositionByChapterPosition(string filePath, string chapter,
        int issueChapterLine,
        CancellationToken ct = default)
    {
        var fileName = Path.GetFileName(filePath);
        var directoryName = Path.GetDirectoryName(filePath) ?? ".";
        var lines = await File.ReadAllLinesAsync(filePath, ct);

        var path = new List<string> { fileName.TrimStart('/') };
        var isPatchChapter = chapter == string.Join(ChapterSeparator, path.Where(e => !string.IsNullOrWhiteSpace(e)));
        var lineNumber = 0;
        var fileLineNumber = 0;
        var includedFiles = new List<string>();
        foreach (var line in lines)
        {
            fileLineNumber++;
            if (line.TryParseCommand(out var command))
            {
                var level = LineLevel(command, out var title);
                if (level <= 3)
                {
                    while (level < path.Count)
                        path.RemoveAt(path.Count - 1);
                    while (level > path.Count)
                        path.Add("");
                    path.Add(title);
                    isPatchChapter =
                        chapter == string.Join(ChapterSeparator, path.Where(e => !string.IsNullOrWhiteSpace(e)));
                }
                else if (command is { Command: "include", Argument: not null })
                {
                    includedFiles.Add(command.Argument);
                }
            }

            if (isPatchChapter)
            {
                lineNumber++;
                if (lineNumber == issueChapterLine)
                    return new FilePosition
                    {
                        Path = filePath,
                        Line = fileLineNumber,
                    };
            }
        }

        return await includedFiles.ToAsyncEnumerable()
            .Select(async (f, _, t) =>
                await FilePositionByChapterPosition($"{directoryName}/{f}.tex".TrimStart('/'), chapter,
                    issueChapterLine, t))
            .Where(e => e.HasValue)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<FileIssue>> IssuesToFileIssuesAsync(string filePath,
        IReadOnlyCollection<Issue> issues, CancellationToken ct = default)
    {
        var fileName = Path.GetFileName(filePath);
        var directoryName = Path.GetDirectoryName(filePath) ?? ".";
        var lines = await File.ReadAllLinesAsync(filePath, ct);

        var result = new List<FileIssue>();
        var path = new List<string> { fileName.TrimStart('/') };
        var currentChapter = string.Join(ChapterSeparator, path.Where(e => !string.IsNullOrWhiteSpace(e)));
        var chapterIssues = issues
            .Where(e => e.Chapter == currentChapter)
            .OrderBy(e => e.Line)
            .ToList();
        var chapterLineNumber = 0;
        var fileLineNumber = 0;
        var includedFiles = new List<string>();
        foreach (var line in lines)
        {
            fileLineNumber++;
            chapterLineNumber++;
            if (line.TryParseCommand(out var command))
            {
                var level = LineLevel(command, out var title);
                if (level <= 3)
                {
                    result.AddRange(chapterIssues.Select(e => new FileIssue(e, null)));

                    while (level < path.Count)
                        path.RemoveAt(path.Count - 1);
                    while (level > path.Count)
                        path.Add("");
                    path.Add(title);

                    currentChapter = string.Join(ChapterSeparator, path.Where(e => !string.IsNullOrWhiteSpace(e)));
                    chapterIssues = issues
                        .Where(e => e.Chapter == currentChapter)
                        .OrderBy(e => e.Line)
                        .ToList();
                    chapterLineNumber = 1;
                }
                else if (command is { Command: "include", Argument: not null })
                {
                    includedFiles.Add(command.Argument);
                }
            }

            while (chapterIssues.Count > 0 && chapterIssues[0].Line == chapterLineNumber)
            {
                result.Add(new FileIssue(chapterIssues[0], new FilePosition
                {
                    Path = filePath,
                    Line = fileLineNumber
                }));
                chapterIssues.RemoveAt(0);
            }
        }

        return await includedFiles.ToAsyncEnumerable()
            .SelectMany<string, FileIssue>(async (f, _, t) =>
                await IssuesToFileIssuesAsync($"{directoryName}/{f}.tex".TrimStart('/'), issues, t))
            .Concat(result.ToAsyncEnumerable())
            .ToListAsync(ct);
    }
}