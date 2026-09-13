using System.IO;
using WinTime.ViewModels;

namespace WinTime.Models;

/// <summary>
/// Статистика приложения для вкладки «Процессы»: общее время работы процесса против активного времени в фокусе.
/// Поддерживает автоматическое обновление в UI в реальном времени.
/// </summary>
public sealed class ProcessUptimeItem : BaseViewModel
{
    private int _uptimeSeconds;
    private int _activeSeconds;
    private bool _isCurrentlyRunning;

    public int AppId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string Category { get; set; } = "Без категории";
    public string? IconBlob { get; set; }
    public bool IsBlacklisted { get; set; }

    public int UptimeSeconds
    {
        get => _uptimeSeconds;
        set
        {
            if (SetProperty(ref _uptimeSeconds, value))
            {
                OnPropertyChanged(nameof(FormattedUptime));
                OnPropertyChanged(nameof(ActiveRatioPercent));
                OnPropertyChanged(nameof(FormattedRatio));
            }
        }
    }

    /// <summary>Время активного фокуса (секунды)</summary>
    public int ActiveSeconds
    {
        get => _activeSeconds;
        set
        {
            if (SetProperty(ref _activeSeconds, value))
            {
                OnPropertyChanged(nameof(FormattedActiveTime));
                OnPropertyChanged(nameof(ActiveRatioPercent));
                OnPropertyChanged(nameof(FormattedRatio));
            }
        }
    }

    /// <summary>Запущен ли процесс прямо сейчас</summary>
    public bool IsCurrentlyRunning
    {
        get => _isCurrentlyRunning;
        set
        {
            if (SetProperty(ref _isCurrentlyRunning, value))
                OnPropertyChanged(nameof(StatusText));
        }
    }

    public string FriendlyName =>
        !string.IsNullOrWhiteSpace(DisplayName)
            ? DisplayName
            : Path.GetFileNameWithoutExtension(ProcessName);

    public string FormattedUptime => FormatDuration(UptimeSeconds);
    public string FormattedActiveTime => FormatDuration(ActiveSeconds);

    /// <summary>Процент активного времени от общего времени работы (0..100)</summary>
    public double ActiveRatioPercent =>
        UptimeSeconds > 0
            ? Math.Min(100.0, Math.Round((double)ActiveSeconds / UptimeSeconds * 100.0, 1))
            : (ActiveSeconds > 0 ? 100.0 : 0.0);

    public string FormattedRatio => $"{ActiveRatioPercent:F0}%";

    public string StatusText => IsCurrentlyRunning ? "Работает" : "Закрыто";

    public void TickUptime()
    {
        UptimeSeconds++;
    }

    public void TickActive()
    {
        ActiveSeconds++;
    }

    private static string FormatDuration(int totalSec)
    {
        if (totalSec <= 0) return "0м";
        var h = totalSec / 3600;
        var m = (totalSec % 3600) / 60;
        var s = totalSec % 60;

        if (h > 0) return $"{h}ч {m}м";
        if (m > 0) return $"{m}м {s}с";
        return $"{s}с";
    }
}
