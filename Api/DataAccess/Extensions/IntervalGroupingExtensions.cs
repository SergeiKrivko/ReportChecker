namespace ReportChecker.DataAccess.Extensions;

public static class IntervalGroupingExtensions
{
    public static IEnumerable<IIntervalResult<T, DateTime>> GroupByIntervals<T>(
        this IEnumerable<T> source,
        Func<T, DateTime> timeSelector,
        DateTime startTime,
        DateTime endTime,
        int numberOfIntervals)
    {
        var intervalSpan = (endTime - startTime).TotalMilliseconds / numberOfIntervals;

        return Enumerable.Range(0, numberOfIntervals)
            .Select(i =>
            {
                var intervalStart = startTime.AddMilliseconds(i * intervalSpan);
                var intervalEnd = startTime.AddMilliseconds((i + 1) * intervalSpan);

                var itemsInInterval = source
                    .Where(item =>
                    {
                        var time = timeSelector(item);
                        return time >= intervalStart && time < intervalEnd;
                    })
                    .ToList();

                return new IntervalResult<T, DateTime>
                {
                    IntervalStart = intervalStart,
                    IntervalEnd = intervalEnd,
                    Items = itemsInInterval
                };
            });
    }

    private class IntervalResult<TSource, TInterval> : IIntervalResult<TSource, TInterval>
    {
        public required TInterval IntervalStart { get; init; }
        public required TInterval IntervalEnd { get; init; }
        public required IReadOnlyList<TSource> Items { get; init; }
    }
}

public interface IIntervalResult<TSource, TInterval>
{
    public TInterval IntervalStart { get; }
    public TInterval IntervalEnd { get; }
    public IReadOnlyList<TSource> Items { get; }
}