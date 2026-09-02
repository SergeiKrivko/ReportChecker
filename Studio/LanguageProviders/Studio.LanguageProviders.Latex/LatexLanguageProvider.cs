using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Models;
using Studio.LanguageProviders.Latex.Completions;
using Studio.LanguageProviders.Latex.Helpers;
using Studio.LanguageProviders.Latex.Models;

namespace Studio.LanguageProviders.Latex;

public class LatexLanguageProvider(string path, IAlertService alertService) : ILanguageProvider
{
    private Dictionary<string, LatexLibrary> _libraries = [];

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly FileParser _fileParser = new(path);

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var stream =
            Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("Studio.LanguageProviders.Latex.Data.LatexCommands.json") ??
            throw new Exception("LaTeX data not found");
        _libraries =
            await JsonSerializer.DeserializeAsync<Dictionary<string, LatexLibrary>>(stream, JsonSerializerOptions,
                ct) ?? [];
    }

    private IEnumerable<LatexCommand> GetCommands()
    {
        return _libraries.SelectMany(e => e.Value.Commands);
    }

    public LanguageCompletions GetCompletions(string triggerText, string fileText, int offset)
    {
        if (triggerText == "\\")
            return new LanguageCompletions(GetCommands()
                .Select(c => new LatexCommandCompletion(c)));
        var currentArgument = FindCurrentArgument(fileText, offset, out var match);
        if (currentArgument != null && match != null)
            switch (currentArgument.Type)
            {
                case "environment":
                    return new LanguageCompletions(_libraries
                            .SelectMany(l => l.Value.Environments)
                            .Select(e => new LatexEnvironmentCompletion(e)), match.ArgumentStartOffset,
                        match.ArgumentEndOffset);
                case "label":
                    return new LanguageCompletions(_fileParser.Labels.Select(e => new LatexLabelCompletion(e)),
                        match.ArgumentStartOffset, match.ArgumentEndOffset);
                case "bibtex":
                    return new LanguageCompletions(_fileParser.Bibliography.Select(e => new LatexBibtexCompletion(e)),
                        match.ArgumentStartOffset, match.ArgumentEndOffset);
            }

        return LanguageCompletions.Empty();
    }

    private LatexArgument? FindCurrentArgument(string fileText, int offset, out LatexArgumentMatch? match)
    {
        var m = match = CurrentCommandParser.GetArgumentAtCursor(fileText, offset);
        if (match == null)
            return null;
        var command = GetCommands().FirstOrDefault(e => e.Name == m?.CommandName);
        if (command == null || match.ArgumentIndex >= command.Arguments.Length)
            return null;
        return command.Arguments[match.ArgumentIndex];
    }

    public async Task<BuildResult> BuildAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var workingDirectory = Path.GetDirectoryName(path) ?? Directory.GetCurrentDirectory();
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "pdflatex",
            ArgumentList = { "-interaction=nonstopmode", "-synctex=1", path },
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
        });
        if (process == null)
        {
            alertService.SendAlert(AlertType.Error, "Не удалось запустить pdflatex");
            return BuildResult.Failure();
        }

        await process.WaitForExitAsync(ct);
        alertService.SendAlert(AlertType.Info, $"pdflatex завершился с кодом {process.ExitCode}");

        var logParser = new PdfLatexLogParser();
        var logFilePath = Path.Combine(workingDirectory, Path.GetFileNameWithoutExtension(path) + ".log");
        var logData = await File.ReadAllTextAsync(logFilePath, ct);
        var problems = logParser.ParseLog(logData, workingDirectory);

        stopwatch.Stop();
        return new BuildResult
        {
            IsSuccess = process.ExitCode == 0,
            Problems = problems,
            Artifacts = process.ExitCode == 0 ? [Path.ChangeExtension(path, "pdf")] : [],
            Duration = stopwatch.Elapsed,
        };
    }

    public async Task ParseAllAsync(CancellationToken ct = default)
    {
        await _fileParser.ParseAllAsync(ct);
    }

    public async Task ParseFileAsync(string p, CancellationToken ct = default)
    {
        if (!_fileParser.IsFileInProject(p))
            return;
        await _fileParser.ParseFileAsync(p, await File.ReadAllTextAsync(p, ct), ct);
    }

    public async Task ParseFileAsync(string p, string data, CancellationToken ct = default)
    {
        if (!_fileParser.IsFileInProject(p))
            return;
        await _fileParser.ParseFileAsync(p, data, ct);
    }

    public IReadOnlyList<LanguageDirectory> GetDirectories()
    {
        var root = Path.GetDirectoryName(path) ?? ".";
        return
        [
            new LanguageDirectory
            {
                Name = "Исходники",
                Files = Directory.EnumerateFiles(root, "*.tex", SearchOption.AllDirectories)
                    .Select(e => new LanguageFile
                    {
                        Name = Path.GetFileName(e),
                        Path = e,
                    })
                    .ToList()
            },
            new LanguageDirectory
            {
                Name = "Вспомогательные файлы",
                Files = Directory.EnumerateFiles(root, "*.aux", SearchOption.AllDirectories)
                    .Concat(Directory.EnumerateFiles(root, "*.toc", SearchOption.AllDirectories))
                    .Concat(Directory.EnumerateFiles(root, "*.log", SearchOption.AllDirectories))
                    .Concat(Directory.EnumerateFiles(root, "*.out", SearchOption.AllDirectories))
                    .Select(e => new LanguageFile
                    {
                        Name = Path.GetFileName(e),
                        Path = e,
                    })
                    .ToList()
            },
            new LanguageDirectory
            {
                Name = "Сборки",
                Files = Directory.EnumerateFiles(root, "*.pdf", SearchOption.AllDirectories)
                    .Select(e => new LanguageFile
                    {
                        Name = Path.GetFileName(e),
                        Path = e,
                    })
                    .ToList()
            },
        ];
    }
}