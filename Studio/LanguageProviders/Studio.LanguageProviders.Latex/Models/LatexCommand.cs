using System.Text.Json.Serialization;

namespace Studio.LanguageProviders.Latex.Models;

public class LatexCommand
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    [JsonPropertyName("args")] public LatexArgument[] Arguments { get; init; } = [];
}