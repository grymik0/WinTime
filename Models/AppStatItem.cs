using System.Windows.Media.Imaging;
using WinTime.ViewModels;

namespace WinTime.Models;

/// <summary>
/// Display model for an application row in the dashboard top list.
/// </summary>
public sealed class AppStatItem : BaseViewModel
{
    private long _totalSeconds;
    private double _percentage;

    public int AppId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    public long TotalSeconds
    {
        get => _totalSeconds;
        set
        {
            if (SetProperty(ref _totalSeconds, value))
                OnPropertyChanged(nameof(FormattedTime));
        }
    }

    public BitmapSource? Icon { get; set; }

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

