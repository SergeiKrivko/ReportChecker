using System.Text;
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
                         ? await archive.OpenAsync() ?? throw new FileNotFoundException("Entry file not found")
                         : await archive.OpenAsync(fileName) ??
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
        await using var entry = await archive.OpenAsync();
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
}