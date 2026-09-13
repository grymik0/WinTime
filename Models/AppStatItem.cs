using System.Windows.Media.Imaging;
using WinTime.ViewModels;

namespace WinTime.Models;

/// <summary>
/// DTO для одной строки в таблице статистики на дашборде.
/// Поддерживает автоматическое обновление в UI при инкременте времени.
/// </summary>
public sealed class AppStatItem : BaseViewModel
{
    private long _totalSeconds;
    private double _percentage;

    public int AppId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Суммарное экранное время в секундах.</summary>
    public long TotalSeconds
    {
        get => _totalSeconds;
        set
        {
            if (SetProperty(ref _totalSeconds, value))
                OnPropertyChanged(nameof(FormattedTime));
        }
    }

    /// <summary>Иконка приложения (загружается лениво через IconService).</summary>
    public BitmapSource? Icon { get; set; }

    /// <summary>Доля от общего времени за период (0–100).</summary>
    public double Percentage
    {
        get => _percentage;
        set => SetProperty(ref _percentage, value);
    }

    public string FormattedTime => FormatSeconds(TotalSeconds);

    public void AddSecond(long newGrandTotal)
    {
        TotalSeconds++;
        if (newGrandTotal > 0)
            Percentage = Math.Round((double)TotalSeconds / newGrandTotal * 100.0, 1);
    }

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
