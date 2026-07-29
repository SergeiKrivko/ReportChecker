using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ReportChecker.Shared.Models;
using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.Converters;

public class AlertColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not AlertType type)
            return null;
        var key = type switch
        {
            AlertType.Info => "PrimaryColor",
            AlertType.Success => "SuccessColor",
            AlertType.Warning => "WarningColor",
            AlertType.Error => "ErrorColor",
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