using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace WinTime.Services;

/// <summary>
/// Извлекает иконки приложений по пути к EXE и кэширует в памяти.
/// Все ошибки перехватываются тихо — возвращается null.
/// </summary>
public sealed class IconService
{
    private readonly Dictionary<string, BitmapSource?> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Возвращает BitmapSource для WPF или null если не удалось извлечь.
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
                    result.Freeze(); // делаем пригодным для использования из любого потока
                }
            }
        }
        catch { /* Нет доступа или файл отсутствует */ }

        _cache[processPath] = result;
        return result;
    }
}
