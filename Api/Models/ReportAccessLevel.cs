namespace ReportChecker.Models;

public enum ReportAccessLevel
{
    Read = 0,
    Comment = 1,
    Edit = 2,
    Owner = 3,
}

public static class ReportAccessLevelExtensions
{
    public static bool Satisfies(this ReportAccessLevel level, ReportAccessLevel required)
    {
        return level >= required;
    }
}
