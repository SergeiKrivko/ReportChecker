using System.IO.Compression;
using System.Text;
using ReportChecker.Cli.Abstractions;
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
            Path.GetFullPath(Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Invalid path"));
        using (var zip = await ZipArchive.CreateAsync(memoryStream, ZipArchiveMode.Create, true, Encoding.UTF8))
        {
            foreach (var file in Directory.EnumerateFiles(rootPath, "*.tex", SearchOption.AllDirectories))
            {
                var entryPath = Path.GetRelativePath(rootPath, file);
                await zip.CreateEntryFromFileAsync(path, entryPath, CompressionLevel.Optimal);
            }
        }

        memoryStream.Seek(0, SeekOrigin.Begin);
        return new SourcePack(memoryStream, Path.GetFileName(Path.ChangeExtension(path, ".zip")),
            Path.GetFileName(path));
    }

    public Task<DateTime> GetUpdateTimeAsync(string path)
    {
        var directoryInfo = new DirectoryInfo(path);
        var time = directoryInfo.EnumerateFiles("*.tex", SearchOption.AllDirectories)
            .Select(e => e.LastWriteTimeUtc)
            .Max();
        return Task.FromResult(time);
    }
}