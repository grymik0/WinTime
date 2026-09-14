using System.Windows;
using System.Windows.Media;

namespace WinTime.Models;

/// <summary>
/// Представляет один день в сетке календаря активности (Heatmap).
/// </summary>
public sealed class HeatmapDayItem
{
    private static readonly Dictionary<string, SolidColorBrush> BrushCache = new();

    public static SolidColorBrush GetBrush(string hex)
    {
        if (!BrushCache.TryGetValue(hex, out var brush))
        {
            brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
            brush.Freeze();
            BrushCache[hex] = brush;
        }
        return brush;
    }

    public DateTime Date { get; set; }
    public long ActiveSeconds { get; set; }
    public int Intensity { get; set; } // 0..4
    public string ColorHex { get; set; } = "#252535";
    public string TooltipText { get; set; } = string.Empty;
    public bool IsFuture { get; set; }
    public bool IsToday { get; set; }

    public SolidColorBrush FillBrush => GetBrush(ColorHex);
    public SolidColorBrush BorderBrush => IsToday ? GetBrush("#F59E0B") : Brushes.Transparent;
    public Thickness BorderThickness => IsToday ? new Thickness(1.5) : new Thickness(0);
}

/// <summary>
/// Колонка одной недели для тепловой карты (содержит до 7 дней: Пн..Вс).
/// </summary>
public sealed class HeatmapWeekItem
{
    public string MonthLabel { get; set; } = string.Empty;
    public List<HeatmapDayItem> Days { get; set; } = [];
}

