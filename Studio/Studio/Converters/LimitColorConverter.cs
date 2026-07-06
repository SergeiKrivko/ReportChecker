using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.Converters;

public class LimitColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Limit<int> limit)
            return null;
        var frac = (double)limit.Current / limit.Maximum;
        var key = "DangerColor";
        if (frac < 0.5)
            key = "SuccessColor";
        else if (frac < 0.8)
            key = "WarningColor";

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