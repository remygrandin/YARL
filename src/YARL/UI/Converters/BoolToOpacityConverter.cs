using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace YARL.UI.Converters;

/// <summary>
/// Returns 0.5 opacity when true (e.g., game is Missing), 1.0 when false.
/// </summary>
public class BoolToOpacityConverter : IValueConverter
{
    public static readonly BoolToOpacityConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? 0.5 : 1.0;
        return 1.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
