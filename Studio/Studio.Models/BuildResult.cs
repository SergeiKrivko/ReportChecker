namespace ReportChecker.Studio.Models;

public class BuildResult
{
    public bool IsSuccess { get; init; }
    public IReadOnlyList<BuildProblem> Problems { get; init; } = [];
    public TimeSpan Duration { get; init; }
    public IReadOnlyList<string> Artifacts { get; init; } = [];

    public static BuildResult Failure() => new BuildResult { IsSuccess = false };
}

public class BuildProblem
{
    public string? FilePath { get; init; }
    public int? LineNumber { get; init; }
    public BuildProblemType Type { get; init; } = BuildProblemType.Other;
    public string? Message { get; init; }
    public string? Source { get; init; }
};

public enum BuildProblemType
{
    Error,
    Warning,
    Hint,
    Other
}