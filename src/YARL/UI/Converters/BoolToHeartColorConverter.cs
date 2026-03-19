using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace YARL.UI.Converters;

/// <summary>
/// Returns accent color (#7C6FF7) when true (favorited), muted color (#8888aa) when false.
/// </summary>
public class BoolToHeartColorConverter : IValueConverter
{
    public static readonly BoolToHeartColorConverter Instance = new();

    private static readonly IBrush AccentBrush = new SolidColorBrush(Color.Parse("#7C6FF7"));
    private static readonly IBrush MutedBrush = new SolidColorBrush(Color.Parse("#8888aa"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b && b ? AccentBrush : MutedBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
