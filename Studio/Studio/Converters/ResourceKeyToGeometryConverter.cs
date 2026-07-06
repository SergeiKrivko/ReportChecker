using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ReportChecker.Studio.Converters;

public class ResourceKeyToGeometryConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string key &&
            Application.Current?.Resources.TryGetResource(key, Application.Current.ActualThemeVariant,
                out var resource) == true)
            return resource as StreamGeometry;

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => null;
}