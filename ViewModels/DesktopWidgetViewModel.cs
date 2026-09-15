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
    private string _sessionTimeText  = "0с";
    private string _userLevelText    = "Ур. 1";
    private string _xpProgressText   = "0 / 1000 XP";
    private double _xpProgress       = 0;
    private string _mouseSummaryText = "0 кл · 0 м";

    public string CurrentAppName   { get => _currentAppName;   private set => SetProperty(ref _currentAppName,   value); }
    public string TodayTimeText    { get => _todayTimeText;    private set => SetProperty(ref _todayTimeText,    value); }
    public string SessionTimeText  { get => _sessionTimeText;  private set => SetProperty(ref _sessionTimeText,  value); }
    public string UserLevelText    { get => _userLevelText;    private set => SetProperty(ref _userLevelText,    value); }
    public string XpProgressText   { get => _xpProgressText;   private set => SetProperty(ref _xpProgressText,   value); }
    public double XpProgress       { get => _xpProgress;       private set => SetProperty(ref _xpProgress,       value); }
    public string MouseSummaryText { get => _mouseSummaryText; private set => SetProperty(ref _mouseSummaryText, value); }

    // Visibility toggles from settings
    public bool ShowApp
    {
        get => _settings.WidgetShowApp;
        set
        {
            if (_settings.WidgetShowApp != value)
            {
                _settings.WidgetShowApp = value;
                OnPropertyChanged(nameof(ShowApp));
            }
        }
    }

    public bool ShowTime
    {
        get => _settings.WidgetShowTime;
        set
        {
            if (_settings.WidgetShowTime != value)
            {
                _settings.WidgetShowTime = value;
                OnPropertyChanged(nameof(ShowTime));
            }
        }
    }

    public bool ShowLevel
    {
        get => _settings.WidgetShowLevel;
        set
        {
            if (_settings.WidgetShowLevel != value)
            {
                _settings.WidgetShowLevel = value;
                OnPropertyChanged(nameof(ShowLevel));
            }
        }
    }

    public bool ShowMouse
    {
        get => _settings.WidgetShowMouse;
        set
        {
            if (_settings.WidgetShowMouse != value)
            {
                _settings.WidgetShowMouse = value;
                OnPropertyChanged(nameof(ShowMouse));
            }
        }
    }

    public bool ShowSession
    {
        get => _settings.WidgetShowSession;
        set
        {
            if (_settings.WidgetShowSession != value)
            {
                _settings.WidgetShowSession = value;
                OnPropertyChanged(nameof(ShowSession));
            }
        }
    }

    public bool IsCompactMode
    {
        get => _settings.WidgetCompactMode;
        set
        {
            if (_settings.WidgetCompactMode != value)
            {
                _settings.WidgetCompactMode = value;
                OnPropertyChanged(nameof(IsCompactMode));
                OnPropertyChanged(nameof(IsNotCompactMode));
                CompactModeChanged?.Invoke(this, value);
            }
        }
    }

    public bool IsNotCompactMode => !IsCompactMode;

    public bool IsClickThrough
    {
        get => _settings.WidgetClickThrough;
        set
        {
            if (_settings.WidgetClickThrough != value)
            {
                _settings.WidgetClickThrough = value;
                OnPropertyChanged(nameof(IsClickThrough));
                ClickThroughChanged?.Invoke(this, value);
            }
        }
    }

    public bool IsTopmost
    {
        get => _settings.WidgetTopmost;
        set
        {
            if (_settings.WidgetTopmost != value)
            {
                _settings.WidgetTopmost = value;
                OnPropertyChanged(nameof(IsTopmost));
                TopmostChanged?.Invoke(this, value);
            }
        }
    }

    public bool IsWidgetEnabled
    {
        get => _settings.ShowWidget;
        set
        {
            if (_settings.ShowWidget != value)
            {
                _settings.ShowWidget = value;
                OnPropertyChanged(nameof(IsWidgetEnabled));
                OnPropertyChanged(nameof(WidgetToggleLabel));
                WidgetVisibilityChanged?.Invoke(this, value);
            }
        }
    }

    public string WidgetToggleLabel => IsWidgetEnabled ? "Скрыть виджет с экрана" : "Включить виджет на рабочем столе";

    public double WidgetOpacity
    {
        get => _settings.WidgetOpacity;
        set
        {
            if (Math.Abs(_settings.WidgetOpacity - value) > 0.01)
            {
                _settings.WidgetOpacity = value;
                OnPropertyChanged(nameof(WidgetOpacity));
                OnPropertyChanged(nameof(WidgetOpacityPercentText));
                WidgetOpacityChanged?.Invoke(this, value);
            }
        }
    }

    public string WidgetOpacityPercentText => $"{(int)Math.Round(WidgetOpacity * 100)}%";

    public event EventHandler<bool>? WidgetVisibilityChanged;
    public event EventHandler<double>? WidgetOpacityChanged;
    public event EventHandler<bool>? CompactModeChanged;
    public event EventHandler<bool>? ClickThroughChanged;
    public event EventHandler<bool>? TopmostChanged;
    public event EventHandler? ResetPositionRequested;

    public ICommand ToggleWidgetCommand  { get; }
    public ICommand ResetPositionCommand { get; }
    public ICommand ToggleCompactCommand { get; }

    private long _todayActiveSeconds;
    private long _continuousSessionSeconds;
    private int  _dbRefreshCounter;

    public DesktopWidgetViewModel(
        ActivityTracker tracker,
        ActivityRepository activityRepo,
        SettingsService settings)
    {
        _tracker      = tracker;
        _activityRepo = activityRepo;
        _settings     = settings;

        ToggleWidgetCommand  = new RelayCommand(() => IsWidgetEnabled = !IsWidgetEnabled);
        ResetPositionCommand = new RelayCommand(() => ResetPositionRequested?.Invoke(this, EventArgs.Empty));
        ToggleCompactCommand = new RelayCommand(() => IsCompactMode = !IsCompactMode);

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
                _continuousSessionSeconds++;
                TodayTimeText = Fmt(_todayActiveSeconds);
                SessionTimeText = Fmt(_continuousSessionSeconds);
                CurrentAppName = string.IsNullOrWhiteSpace(e.AppName) ? "Активность" : e.AppName;
            }
            else
            {
                _continuousSessionSeconds = 0;
                SessionTimeText = "0с";
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
