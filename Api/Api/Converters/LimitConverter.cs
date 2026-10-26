using ReportChecker.Api.Dtos;
using ReportChecker.Models;

namespace ReportChecker.Api.Converters;

public static class LimitConverter
{
    public static LimitDto<T> ToDto<T>(this Limit<T> limit) where T : IComparable<T>, IEquatable<T>
    {
        return new LimitDto<T>
        {
            Current = limit.Current,
            Maximum = limit.Maximum,
        };
    }
}