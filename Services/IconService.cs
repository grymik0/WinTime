using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace WinTime.Services;

/// <summary>
/// Extracts and caches application process icons.
/// </summary>
public sealed class IconService
{
    private readonly Dictionary<string, BitmapSource?> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns BitmapSource representation of the process executable icon or null if extraction fails.
    /// </summary>
    public BitmapSource? GetIcon(string? processPath)
    {
        if (string.IsNullOrEmpty(processPath)) return null;
        if (_cache.TryGetValue(processPath, out var cached)) return cached;

        BitmapSource? result = null;
        try
        {
            if (File.Exists(processPath))
            {
                using var icon = Icon.ExtractAssociatedIcon(processPath);
                if (icon is not null)
                {
                    result = Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    result.Freeze();
                }
            }
        }
        catch { /* ignored */ }

        _cache[processPath] = result;
        return result;
    }
}

