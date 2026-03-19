using System;
using System.Globalization;
using Avalonia.Data.Converters;
using YARL.Domain.Enums;

namespace YARL.UI.Converters;

/// <summary>
/// Returns true (visible) when GameStatus is Missing, false (collapsed) otherwise.
/// Use with IsVisible binding to show warning icon on missing game tiles.
/// </summary>
public class GameStatusToVisibilityConverter : IValueConverter
{
    public static readonly GameStatusToVisibilityConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is GameStatus status && status == GameStatus.Missing;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
