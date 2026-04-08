using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ReportChecker.Abstractions;
using ReportChecker.Models;
using IFormatProvider = ReportChecker.Abstractions.IFormatProvider;

namespace ReportChecker.FormatProviders.Latex;

public class LatexFormatProvider(IConfiguration configuration) : IFormatProvider
{
    public string Key => "Latex";

    private string ChapterSeparator { get; } = configuration["Reports.ChapterSeparator"] ?? "//";

    public async Task<IEnumerable<Chapter>> GetChaptersAsync(IFileArchive archive)
    {
        try
        {
            return await ParseFileAsync(null, archive).ToListAsync();
        }
        catch (FileNotFoundException)
        {
            return await ParseFileAsync($"{archive.EntryFilePath}/report.tex", archive).ToListAsync();
        }
    }

    private const string IncludePrefix = "\\include{";
    private const string IncludeSuffix = "}";

    private async IAsyncEnumerable<Chapter> ParseFileAsync(string? fileName, IFileArchive archive)
    {
        string text;
        await using (var entryStream = fileName == null
                         ? await archive.ReadAsync() ?? throw new FileNotFoundException("Entry file not found")
                         : await archive.ReadAsync(fileName) ??
                           throw new FileNotFoundException($"File '{fileName}' not found"))
        {
            text = await new StreamReader(entryStream).ReadToEndAsync();
        }

        var path = new List<string> { fileName?.TrimStart('/') ?? "<root>" };
        var builder = new StringBuilder();
        foreach (var line in text.Split('\n'))
        {
            builder.Append(line);
            builder.Append('\n');
            var level = LineLevel(line, out var title);
            if (level <= 3)
            {
                if (builder.Length > 0)
                    yield return new Chapter
                    {
                        Name = string.Join(ChapterSeparator, path.Where(e => !string.IsNullOrWhiteSpace(e))),
                        Content = builder.ToString(),
                    };
                builder.Clear();

                while (level < path.Count)
                    path.RemoveAt(path.Count - 1);
                while (level > path.Count)
                    path.Add("");
                path.Add(title);
            }
        }

        if (builder.Length > 0)
            yield return new Chapter
            {
                Name = string.Join(ChapterSeparator, path.Where(e => !string.IsNullOrWhiteSpace(e))),
                Content = builder.ToString(),
            };

        foreach (var line in text.Split('\n').Select(e => e.Trim()))
            if (line.StartsWith(IncludePrefix) && line.EndsWith(IncludeSuffix))
            {
                var includeFileName = line.Substring(IncludePrefix.Length,
                    line.Length - IncludePrefix.Length - IncludeSuffix.Length);
                await foreach (var chapter in ParseFileAsync(
                                   $"{Path.GetDirectoryName(fileName)}/{includeFileName}.tex".TrimStart('/'),
                                   archive))
                    yield return chapter;
            }
    }

    public async Task<bool> TestSourceAsync(IFileArchive archive)
    {
        var entryPath = archive.EntryFilePath;
        if (entryPath == null || !entryPath.EndsWith(".tex"))
            return false;
        await using var entry = await archive.ReadAsync();
        return entry != null;
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

    public async Task ApplyPatchAsync(IFileArchive archive, string chapter, IEnumerable<PatchLine> lines,
        CancellationToken ct = default)
    {
        var l = lines.ToList();
        try
        {
            await ApplyPatchAsync(null, archive, chapter, l, ct);
        }
        catch (FileNotFoundException)
        {
            await ApplyPatchAsync($"{archive.EntryFilePath}/report.tex", archive, chapter, l, ct);
        }
    }

    private async Task<bool> ApplyPatchAsync(string? fileName, IFileArchive archive, string chapter,
        IReadOnlyList<PatchLine> lines, CancellationToken ct)
    {
        string text;
        await using (var entryStream = fileName == null
                         ? await archive.ReadAsync() ?? throw new FileNotFoundException("Entry file not found")
                         : await archive.ReadAsync(fileName) ??
                           throw new FileNotFoundException($"File '{fileName}' not found"))
        {
            text = await new StreamReader(entryStream).ReadToEndAsync(ct);
        }

        var patchApplied = false;
        var path = new List<string> { fileName?.TrimStart('/') ?? "<root>" };
        var isPatchChapter = chapter == string.Join(ChapterSeparator, path.Where(e => !string.IsNullOrWhiteSpace(e)));
        var lineNumber = 0;
        var builder = new StringBuilder();
        foreach (var line in text.Split('\n'))
        {
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
        }

        if (patchApplied)
        {
            var stream = new MemoryStream();
            await using (var writer = new StreamWriter(stream))
            {
                text = builder.ToString();
                var span = text.EndsWith("\r\n") ? text.AsSpan(0, text.Length - 2) : text.AsSpan(0, text.Length - 1);
                await writer.WriteAsync(span.ToString());
            }

            stream = new MemoryStream(stream.ToArray());
            if (fileName == null)
                await archive.WriteAsync(stream, ct);
            else
                await archive.WriteAsync(fileName, stream, ct);
            return true;
        }

        foreach (var line in text.Split('\n').Select(e => e.Trim()))
            if (line.StartsWith(IncludePrefix) && line.EndsWith(IncludeSuffix))
            {
                var includeFileName = line.Substring(IncludePrefix.Length,
                    line.Length - IncludePrefix.Length - IncludeSuffix.Length);
                if (await ApplyPatchAsync($"{Path.GetDirectoryName(fileName)}/{includeFileName}.tex".TrimStart('/'),
                        archive, chapter, lines, ct))
                    return true;
            }

        return false;
    }
}