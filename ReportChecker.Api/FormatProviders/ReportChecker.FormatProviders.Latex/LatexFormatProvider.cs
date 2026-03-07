using ReportChecker.Abstractions;
using ReportChecker.Models;
using IFormatProvider = ReportChecker.Abstractions.IFormatProvider;

namespace ReportChecker.FormatProviders.Latex;

public class LatexFormatProvider : IFormatProvider
{
    public string Key => "Latex";

    public async Task<IEnumerable<Chapter>> GetChaptersAsync(IFileArchive archive)
    {
        return await ParseFileAsync("report.tex", archive).ToListAsync();
    }

    private const string IncludePrefix = "\\include{";
    private const string IncludeSuffix = "}";

    private static async IAsyncEnumerable<Chapter> ParseFileAsync(string fileName, IFileArchive archive)
    {
        string text;
        await using (var entryStream = await archive.OpenAsync(fileName) ?? throw new FileNotFoundException())
        {
            text = await new StreamReader(entryStream).ReadToEndAsync();
        }

        yield return new Chapter
        {
            Name = fileName,
            Content = text,
        };

        foreach (var line in text.Split('\n').Select(e => e.Trim()))
            if (line.StartsWith(IncludePrefix) && line.EndsWith(IncludeSuffix))
            {
                var includeFileName = line.Substring(IncludePrefix.Length,
                    line.Length - IncludePrefix.Length - IncludeSuffix.Length);
                await foreach (var chapter in ParseFileAsync(includeFileName + ".tex", archive))
                    yield return chapter;
            }
    }

    public async Task<bool> TestSourceAsync(IFileArchive archive)
    {
        await using var entry = await archive.OpenAsync("report.tex");
        return entry != null;
    }
}