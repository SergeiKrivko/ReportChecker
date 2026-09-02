using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Lucide.Avalonia;
using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.Converters;

public class AlertIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not AlertType type)
            return null;
        return type switch
        {
            AlertType.Info => LucideIconKind.Info,
            AlertType.Success => LucideIconKind.Check,
            AlertType.Warning => LucideIconKind.TriangleAlert,
            AlertType.Error => LucideIconKind.CircleAlert,
            _ => LucideIconKind.AArrowDown,
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => null;
}