using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Material.Icons;

namespace YARL.UI.Converters;

/// <summary>
/// Returns MaterialIconKind.Heart when true (favorited), MaterialIconKind.HeartOutline when false.
/// </summary>
public class BoolToHeartIconConverter : IValueConverter
{
    public static readonly BoolToHeartIconConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b && b ? MaterialIconKind.Heart : MaterialIconKind.HeartOutline;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
