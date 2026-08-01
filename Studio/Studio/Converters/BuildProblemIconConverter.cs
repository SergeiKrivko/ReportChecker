using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.Converters;

public class BuildProblemIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not BuildProblemType type)
            return null;
        var key = type switch
        {
            BuildProblemType.Error => "IconShieldAlert",
            BuildProblemType.Warning => "IconTriangleAlert",
            BuildProblemType.Hint => "IconTriangleAlert",
            _ => "IconEmpty",
        };
        if (Application.Current?.Resources.TryGetResource(key, Application.Current.ActualThemeVariant,
                out var resource) == true)
            return resource as StreamGeometry;

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => null;
}