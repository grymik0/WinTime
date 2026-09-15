using System.Windows.Input;
using WinTime.Core;
using WinTime.Data;
using WinTime.Services;

namespace WinTime.ViewModels;

public sealed class DesktopWidgetViewModel : BaseViewModel
{
    private readonly ActivityTracker    _tracker;
    private readonly ActivityRepository _activityRepo;
    private readonly SettingsService   _settings;

    private string _currentAppName   = "Ожидание...";
    private string _todayTimeText    = "0с";
    private string _userLevelText    = "Ур. 1";
    private string _xpProgressText   = "0 / 1000 XP";
    private double _xpProgress       = 0;
    private string _mouseSummaryText = "0 кл · 0 м";

    public string CurrentAppName   { get => _currentAppName;   private set => SetProperty(ref _currentAppName,   value); }
    public string TodayTimeText    { get => _todayTimeText;    private set => SetProperty(ref _todayTimeText,    value); }
    public string UserLevelText    { get => _userLevelText;    private set => SetProperty(ref _userLevelText,    value); }
    public string XpProgressText   { get => _xpProgressText;   private set => SetProperty(ref _xpProgressText,   value); }
    public double XpProgress       { get => _xpProgress;       private set => SetProperty(ref _xpProgress,       value); }
    public string MouseSummaryText { get => _mouseSummaryText; private set => SetProperty(ref _mouseSummaryText, value); }

    // Visibility toggles from settings
    public bool ShowApp   => _settings.WidgetShowApp;
    public bool ShowTime  => _settings.WidgetShowTime;
    public bool ShowLevel => _settings.WidgetShowLevel;
    public bool ShowMouse => _settings.WidgetShowMouse;

    private long _todayActiveSeconds;
    private int  _dbRefreshCounter;

    public DesktopWidgetViewModel(
        ActivityTracker tracker,
        ActivityRepository activityRepo,
        SettingsService settings)
    {
        _tracker      = tracker;
        _activityRepo = activityRepo;
        _settings     = settings;

        _tracker.StateChanged += OnTrackerStateChanged;
    }

    public void NotifySettingsChanged()
    {
        OnPropertyChanged(nameof(ShowApp));
        OnPropertyChanged(nameof(ShowTime));
        OnPropertyChanged(nameof(ShowLevel));
        OnPropertyChanged(nameof(ShowMouse));
    }

    public async Task InitializeAsync()
    {
        try
        {
            var (active, _) = await _activityRepo.GetTotalsAsync(DateTime.Today, DateTime.Today.AddDays(1));
            _todayActiveSeconds = active;
            TodayTimeText = Fmt(active);

            await UpdateLifetimeAndLevelAsync();

            var (clicks, dist) = _tracker.GetTodayMouseMetrics();
            MouseSummaryText = $"{DashboardViewModel.FormatClicks(clicks)} кл · {DashboardViewModel.FormatDistance(dist)}";
        }
        catch { }
    }

    private void OnTrackerStateChanged(object? sender, TrackerStateEventArgs e)
    {
        System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            if (!e.IsIdle)
            {
                _todayActiveSeconds++;
                TodayTimeText = Fmt(_todayActiveSeconds);
                CurrentAppName = string.IsNullOrWhiteSpace(e.AppName) ? "Активность" : e.AppName;
            }
            else
            {
                CurrentAppName = "💤 AFK / Бездействие";
            }

            var (clicks, dist) = _tracker.GetTodayMouseMetrics();
            MouseSummaryText = $"{DashboardViewModel.FormatClicks(clicks)} кл · {DashboardViewModel.FormatDistance(dist)}";

            if (++_dbRefreshCounter >= 60)
            {
                _dbRefreshCounter = 0;
                _ = UpdateLifetimeAndLevelAsync();
            }
        });
    }

    private async Task UpdateLifetimeAndLevelAsync()
    {
        try
        {
            var (lifetime, _, _, _, _) = await _activityRepo.GetLifetimeStatsAsync();
            long totalXp = (lifetime / 60) * 10;
            const int xpPerLevel = 1000;
            int level = (int)(totalXp / xpPerLevel) + 1;
            long curLvlXp = totalXp % xpPerLevel;

            UserLevelText = $"Ур. {level}";
            XpProgressText = $"{curLvlXp} / {xpPerLevel} XP";
            XpProgress = Math.Clamp((double)curLvlXp / xpPerLevel * 100.0, 0, 100);
        }
        catch { }
    }

    private static string Fmt(long s)
    {
        var ts = TimeSpan.FromSeconds(s);
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}ч {ts.Minutes:D2}м";
        if (ts.TotalMinutes >= 1) return $"{ts.Minutes}м {ts.Seconds:D2}с";
        return $"{ts.Seconds}с";
    }
}
