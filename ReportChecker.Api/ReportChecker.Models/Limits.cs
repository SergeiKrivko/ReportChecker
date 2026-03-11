namespace ReportChecker.Models;

public class Limits
{
    public required Limit<int> Reports { get; init; }
    public required Limit<int> Checks { get; init; }
    public required Limit<int> Comments { get; init; }
}

public class Limit<T> where T : IComparable<T>, IEquatable<T>
{
    public required T Current { get; init; }
    public required T Maximum { get; init; }
}