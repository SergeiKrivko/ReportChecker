using System.IO.Compression;
using System.Text;
using ReportChecker.Cli.Abstractions;
using ReportChecker.Cli.Models;
using IFormatProvider = ReportChecker.Cli.Abstractions.IFormatProvider;

namespace Cli.FormatProviders.Latex;

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
        return new SourcePack(memoryStream, Path.GetFileName(Path.ChangeExtension(path, ".zip")),
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

    private static int LineLevel(string line, out string title)
    {
        line = line.Trim();
        if (line.StartsWith("\\chapter{") && line.EndsWith('}'))
        {
            title = line.Substring("\\chapter{".Length).TrimEnd('}');
            return 0;
        }

        if (line.StartsWith("\\section{") && line.EndsWith('}'))
        {
            title = line.Substring("\\section{".Length).TrimEnd('}');
            return 1;
        }

        if (line.StartsWith("\\subsection{") && line.EndsWith('}'))
        {
            title = line.Substring("\\subsection{".Length).TrimEnd('}');
            return 2;
        }

        if (line.StartsWith("\\subsubsection{") && line.EndsWith('}'))
        {
            title = line.Substring("\\subsubsection{".Length).TrimEnd('}');
            return 3;
        }

        title = line;
        return int.MaxValue;
    }

    private const string ChapterSeparator = "//";
    private const string IncludePrefix = "\\include{";
    private const string IncludeSuffix = "}";

    public async Task ApplyPatchAsync(string path, string chapter, IEnumerable<PatchLine> lines, CancellationToken ct = default)
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
        var path = new List<string> { fileName?.TrimStart('/') ?? "<root>" };
        var isPatchChapter = chapter == string.Join(ChapterSeparator, path.Where(e => !string.IsNullOrWhiteSpace(e)));
        var lineNumber = 0;
        var builder = new StringBuilder();
        foreach (var line in lst)
        {
            var level = LineLevel(line, out var title);
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

        foreach (var line in text.Split('\n').Select(e => e.Trim()))
            if (line.StartsWith(IncludePrefix) && line.EndsWith(IncludeSuffix))
            {
                var includeFileName = line.Substring(IncludePrefix.Length,
                    line.Length - IncludePrefix.Length - IncludeSuffix.Length);
                var flag = await _ApplyPatchAsync(
                    $"{directoryName}/{includeFileName}.tex".TrimStart('/'), chapter, lines, ct);
                if (flag)
                    return flag;
            }

        return false;
    }
}