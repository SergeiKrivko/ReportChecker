using System.Text;
using Microsoft.Extensions.Configuration;
using ReportChecker.Abstractions;
using ReportChecker.Models;
using ReportChecker.Models.Sources;
using IFormatProvider = ReportChecker.Abstractions.IFormatProvider;

namespace ReportChecker.FormatProviders.Latex;

public class LatexFormatProvider(IConfiguration configuration) : IFormatProvider
{
    public string Key => "Latex";

    private string ChapterSeparator { get; } = configuration["Reports.ChapterSeparator"] ?? "//";

    public async Task<IEnumerable<Chapter>> GetChaptersAsync(IFileArchive archive)
    {
        return await ParseFileAsync(null, new LatexContext
        {
            Archive = archive,
        }).ToListAsync();
    }

    private async IAsyncEnumerable<Chapter> ParseFileAsync(string? fileName, LatexContext context)
    {
        string text;
        context.FileName = fileName;
        await using (var entryStream = fileName == null
                         ? await context.Archive.ReadAsync() ??
                           throw new FileNotFoundException("Entry file not found")
                         : await context.Archive.ReadAsync(fileName) ??
                           throw new FileNotFoundException($"File '{fileName}' not found"))
        {
            text = await new StreamReader(entryStream).ReadToEndAsync();
        }

        var path = new List<string> { fileName?.TrimStart('/') ?? "<root>" };
        var builder = new StringBuilder();
        var images = new List<ChapterImage>();
        var includedFiles = new List<string>();
        foreach (var line in text.Split('\n'))
        {
            if (line.TryParseCommand(out var command))
            {
                Console.WriteLine($"\\{command.Command} [ {command.Options} ] {{ {command.Argument} }}");
                var level = LineLevel(command, out var title);
                if (level <= 3)
                {
                    if (builder.Length > 0)
                    {
                        Console.WriteLine($"{string.Join(ChapterSeparator, path.Where(e => !string.IsNullOrWhiteSpace(e)))} --- {images.Count} images");
                        Console.WriteLine(builder);
                        yield return new Chapter
                        {
                            Name = string.Join(ChapterSeparator, path.Where(e => !string.IsNullOrWhiteSpace(e))),
                            Content = builder.ToString(),
                            Images = images.ToArray(),
                        };
                    }

                    builder.Clear();
                    images.Clear();

                    while (level < path.Count)
                        path.RemoveAt(path.Count - 1);
                    while (level > path.Count)
                        path.Add("");
                    path.Add(title);
                }
                else if (command.Command == "includegraphics")
                {
                    var image = await ProcessImage(command, context);
                    if (image != null)
                        images.Add(image);
                }
                else if (command.Command == "graphicspath")
                {
                    context.ImagesPath = command.Argument?.TrimEnd('/');
                }
                else if (command is { Command: "include", Argument: not null })
                {
                    includedFiles.Add(command.Argument);
                }
            }

            builder.AppendLine(line);
        }

        if (builder.Length > 0)
            yield return new Chapter
            {
                Name = string.Join(ChapterSeparator, path.Where(e => !string.IsNullOrWhiteSpace(e))),
                Content = builder.ToString(),
                Images = images.ToArray(),
            };

        foreach (var file in includedFiles)
        await foreach (var chapter in ParseFileAsync(
                           $"{Path.GetDirectoryName(fileName)}/{file}.tex".TrimStart('/'),
                           context))
            yield return chapter;
    }

    private static async Task<ChapterImage?> ProcessImage(LatexCommand command, LatexContext context)
    {
        if (command.Argument == null)
            return null;
        var includeFileName = command.Argument;
        var mimeType = Path.GetExtension(includeFileName) switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpg",
            ".svg" => "image/svg",
            _ => null,
        };
        await using var imageStream = await context.Archive.ReadAsync(
            $"{context.ImagesPath ?? Path.GetDirectoryName(context.FileName)}/{includeFileName}".TrimStart('/'));
        if (imageStream == null || mimeType == null)
            return null;
        using var memoryStream = new MemoryStream();
        await imageStream.CopyToAsync(memoryStream);

        return new ChapterImage
        {
            Data = memoryStream.ToArray(),
            MimeType = mimeType
        };
    }

    public async Task<bool> TestSourceAsync(IFileArchive archive)
    {
        var entryPath = archive.EntryFilePath;
        if (entryPath == null || !entryPath.EndsWith(".tex"))
            return false;
        await using var entry = await archive.ReadAsync();
        return entry != null;
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

    public async Task<CheckSourceUnion?> ApplyPatchAsync(IFileArchive archive, string chapter,
        IEnumerable<PatchLine> lines,
        CancellationToken ct = default)
    {
        var l = lines.ToList();
        try
        {
            var (_, source) = await ApplyPatchAsync(null, archive, chapter, l, ct);
            return source;
        }
        catch (FileNotFoundException)
        {
            var (_, source) = await ApplyPatchAsync($"{archive.EntryFilePath}/report.tex", archive, chapter, l, ct);
            return source;
        }
    }

    private async Task<(bool, CheckSourceUnion?)> ApplyPatchAsync(string? fileName, IFileArchive archive,
        string chapter,
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

        var lst = new List<string>();
        using (var reader = new StringReader(text))
        {
            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                lst.Add(line);
            }
        }

        var includedFiles = new List<string>();
        var patchApplied = false;
        var path = new List<string> { fileName?.TrimStart('/') ?? "<root>" };
        var isPatchChapter =
            chapter == string.Join(ChapterSeparator, path.Where(e => !string.IsNullOrWhiteSpace(e)));
        var lineNumber = 0;
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
            }
            else if (command is { Command: "include", Argument: not null })
            {
                includedFiles.Add(command.Argument);
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
            var stream = new MemoryStream();
            await using (var writer = new StreamWriter(stream))
            {
                var newText = builder.ToString();
                var span = text.EndsWith('\n')
                    ? newText.AsSpan()
                    : newText.EndsWith("\r\n")
                        ? newText.AsSpan(0, newText.Length - 2)
                        : newText.AsSpan(0, newText.Length - 1);
                await writer.WriteAsync(span.ToString());
            }

            stream = new MemoryStream(stream.ToArray());
            return (true,
                fileName == null
                    ? await archive.WriteAsync(stream, ct)
                    : await archive.WriteAsync(fileName, stream, ct));
        }

        foreach (var file in includedFiles)
        {
            var (flag, source) = await ApplyPatchAsync(
                $"{Path.GetDirectoryName(fileName)}/{file}.tex".TrimStart('/'),
                archive, chapter, lines, ct);
            if (flag)
                return (flag, source);
        }

        return (false, null);
    }
}