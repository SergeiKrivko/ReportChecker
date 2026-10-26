namespace ReportChecker.Api.Dtos;

public class LimitDto<T> where T : IComparable<T>, IEquatable<T>
{
    public required T Current { get; init; }
    public required T Maximum { get; init; }
}