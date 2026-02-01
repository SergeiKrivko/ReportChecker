using System.IO.Compression;
using ReportChecker.Models;
using IFormatProvider = ReportChecker.Abstractions.IFormatProvider;

namespace ReportChecker.FormatProviders.Latex;

public class LatexFormatProvider : IFormatProvider
{
    public string Key => "Latex";

    public async Task<IEnumerable<Chapter>> GetChaptersAsync(Stream sourceStream)
    {
        await using var zip = new ZipArchive(sourceStream, ZipArchiveMode.Read);
        return await ParseFileAsync("report.tex", zip).ToListAsync();
    }

    private const string IncludePrefix = "\\include{";
    private const string IncludeSuffix = "}";

    private static async IAsyncEnumerable<Chapter> ParseFileAsync(string fileName, ZipArchive zip)
    {
        string text;
        var entry = zip.GetEntry(fileName) ?? throw new FileNotFoundException();
        await using (var entryStream = await entry.OpenAsync())
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
                await foreach (var chapter in ParseFileAsync(includeFileName + ".tex", zip))
                    yield return chapter;
            }
    }
}