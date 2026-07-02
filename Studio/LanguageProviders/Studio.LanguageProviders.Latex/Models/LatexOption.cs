namespace Studio.LanguageProviders.Latex.Models;

public class LatexOption
{
    public required string Name { get; init; }
    public string Type { get; init; } = "Text";
    public string? Description { get; init; }
}