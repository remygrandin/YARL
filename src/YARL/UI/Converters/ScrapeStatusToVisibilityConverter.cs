using System;
using System.Globalization;
using Avalonia.Data.Converters;
using YARL.Domain.Enums;

namespace YARL.UI.Converters;

/// <summary>
/// Returns true (visible) when ScrapeStatus is Unmatched — shows "No art" badge.
/// </summary>
public class ScrapeStatusToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is ScrapeStatus status && status == ScrapeStatus.Unmatched;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
