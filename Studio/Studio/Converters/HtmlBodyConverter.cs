using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ReportChecker.Studio.Converters;

public class HtmlBodyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string html)
            return null;
        return $"<body>{html}</body>";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }
}