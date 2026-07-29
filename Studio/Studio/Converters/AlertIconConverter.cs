using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.Converters;

public class AlertIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not AlertType type)
            return null;
        var key = type switch
        {
            AlertType.Info => "IconInfo",
            AlertType.Success => "IconCheckmark",
            AlertType.Warning => "IconWarning",
            AlertType.Error => "IconWarning",
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