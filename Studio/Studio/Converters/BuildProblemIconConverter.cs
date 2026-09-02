using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Lucide.Avalonia;
using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.Converters;

public class BuildProblemIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not BuildProblemType type)
            return null;
        return type switch
        {
            BuildProblemType.Error => LucideIconKind.ShieldAlert,
            BuildProblemType.Warning => LucideIconKind.TriangleAlert,
            BuildProblemType.Hint => LucideIconKind.CircleAlert,
            _ => LucideIconKind.OctagonAlert,
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => null;
}