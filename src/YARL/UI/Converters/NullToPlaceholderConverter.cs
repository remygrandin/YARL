using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace YARL.UI.Converters;

/// <summary>
/// Returns true (visible) when the value is null or empty — shows placeholder.
/// Returns false (hidden) when the value is non-null — hides placeholder.
/// </summary>
public class NullToPlaceholderConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is null or "";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
