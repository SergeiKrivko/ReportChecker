using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ReportChecker.Shared.Models;

namespace ReportChecker.Studio.Converters;

public class IssueColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Issue issue)
            return null;
        var key = issue.Status switch
        {
            IssueStatus.Open => issue.Priority switch
            {
                1 => "DangerColor",
                2 => "DangerColor",
                3 => "WarningColor",
                4 => "WarningColor",
                5 => "WarningColor",
                _ => "PrimaryColor",
            },
            IssueStatus.Closed => "BorderColor",
            IssueStatus.Fixed => "SuccessColor",
            _ => ""
        };
        if (Application.Current?.Resources.TryGetResource(key, Application.Current.ActualThemeVariant,
                out var resource) == true && resource is Color color)
        {
            IBrush brush = new SolidColorBrush(color);
            return brush;
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}