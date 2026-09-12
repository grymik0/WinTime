using System.Windows.Media.Imaging;

namespace WinTime.Models;

/// <summary>
/// DTO для одной строки в таблице статистики на дашборде.
/// </summary>
public sealed class AppStatItem
{
    public int AppId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Суммарное экранное время в секундах.</summary>
    public long TotalSeconds { get; set; }

    /// <summary>Иконка приложения (загружается лениво через IconService).</summary>
    public BitmapSource? Icon { get; set; }

    /// <summary>Доля от общего времени за период (0–100).</summary>
    public double Percentage { get; set; }

    public string FormattedTime => FormatSeconds(TotalSeconds);

    private static string FormatSeconds(long s)
    {
        var ts = TimeSpan.FromSeconds(s);
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}ч {ts.Minutes:D2}м";
        if (ts.TotalMinutes >= 1)
            return $"{ts.Minutes}м {ts.Seconds:D2}с";
        return $"{ts.Seconds}с";
    }
}

