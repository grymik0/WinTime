using WinTime.ViewModels;

namespace WinTime.Models;

/// <summary>
/// Display model for an application window or browser tab title item.
/// </summary>
public sealed class WindowTitleStatItem : BaseViewModel
{
    private long _totalSeconds;
    private double _percentage;

    public string RawTitle { get; set; } = string.Empty;
    public string DisplayTitle { get; set; } = string.Empty;

    public long TotalSeconds
    {
        get => _totalSeconds;
        set
        {
            if (SetProperty(ref _totalSeconds, value))
                OnPropertyChanged(nameof(FormattedTime));
        }
    }

    public double Percentage
    {
        get => _percentage;
        set => SetProperty(ref _percentage, value);
    }

    public string FormattedTime => FormatDuration(TotalSeconds);

    private static string FormatDuration(long totalSec)
    {
        if (totalSec <= 0) return "0с";
        var h = totalSec / 3600;
        var m = (totalSec % 3600) / 60;
        var s = totalSec % 60;

        if (h > 0) return $"{h}ч {m}м";
        if (m > 0) return $"{m}м {s}с";
        return $"{s}с";
    }
}

