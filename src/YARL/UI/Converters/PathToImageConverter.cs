using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace YARL.UI.Converters;

/// <summary>
/// Converts a local file path (string) to a Bitmap for small thumbnail display.
/// Returns null if path is null/empty or file does not exist.
/// For large grid tiles, use AdvancedImage control instead (async + cached).
/// </summary>
public class PathToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrEmpty(path))
            return null;

        try
        {
            if (!File.Exists(path)) return null;
            return new Bitmap(path);
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
