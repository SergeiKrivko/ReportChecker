using System.Reflection;
using System.Text.Json;
using ReportChecker.Studio.Abstractions;
using Studio.LanguageProviders.Latex.Completions;
using Studio.LanguageProviders.Latex.Models;

namespace Studio.LanguageProviders.Latex;

public class LatexLanguageProvider(string path) : ILanguageProvider
{
    private Dictionary<string, LatexLibrary> _libraries = [];

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Studio.LanguageProviders.Latex.Data.LatexCommands.json") ??
                                 throw new Exception("LaTeX data not found");
        _libraries =
            await JsonSerializer.DeserializeAsync<Dictionary<string, LatexLibrary>>(stream, JsonSerializerOptions,
                ct) ?? [];
    }

    public IReadOnlyList<ILanguageCompletion> GetCompletions(string triggerText)
    {
        if (triggerText != "\\")
            return [];
        return _libraries
            .SelectMany(e => e.Value.Commands
                .Select(c => new LatexCommandCompletion(c)))
            .ToList();
    }
}