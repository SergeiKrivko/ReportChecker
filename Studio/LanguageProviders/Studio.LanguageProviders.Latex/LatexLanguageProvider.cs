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
                        .Select(e => new LatexEnvironmentCompletion(e)), match.ArgumentStartOffset, match.ArgumentEndOffset);
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
        Console.WriteLine($"pdflatex {string.Join(' ', process?.StartInfo.ArgumentList ?? [])}");
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
}