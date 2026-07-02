namespace Studio.LanguageProviders.Latex.Models;

public class LatexArgument
{
    public string? Name { get; init; }
    public string Type { get; init; } = "text";
    public string? Description { get; init; }
    public bool Optional { get; init; }
    public string[]? Extensions { get; init; }
}