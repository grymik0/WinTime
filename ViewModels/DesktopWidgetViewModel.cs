using System.Windows.Input;
using WinTime.Core;
using WinTime.Data;
using WinTime.Services;

namespace WinTime.ViewModels;

public sealed class DesktopWidgetViewModel : BaseViewModel
{
    private readonly ActivityTracker      _tracker;
    private readonly ActivityRepository   _activityRepo;
    private readonly SettingsService     _settings;
    private readonly LimitEnforcerService _limitEnforcer;
    private readonly DailyQuestEngine     _questEngine;
    private readonly LocalizationService  _localization;

    private string _currentAppName   = LocalizationService.IsRussian ? "Ожидание..." : "Waiting...";
    private string _todayTimeText    = LocalizationService.FormatDuration(0);
    private string _sessionTimeText  = LocalizationService.FormatDuration(0);
    private int    _currentUserLevel = 1;
    private string _userLevelText    = LocalizationService.IsRussian ? "Ур. 1" : "Lvl 1";
    private string _xpProgressText   = "0 / 1000 XP";
    private double _xpProgress       = 0;
    private string _mouseSummaryText = LocalizationService.IsRussian ? "0 кл · 0 м" : "0 clicks · 0 m";

    private int    _currentStreak;
    private bool   _hasAppLimit;
    private bool   _isLimitExceeded;
    private bool   _isLimitWarning;
    private string _limitRemainingText = string.Empty;
    private string _limitStatusText    = string.Empty;
    private double _limitProgress;

    public string CurrentAppName   { get => _currentAppName;   private set => SetProperty(ref _currentAppName,   value); }
    public string TodayTimeText    { get => _todayTimeText;    private set => SetProperty(ref _todayTimeText,    value); }
    public string SessionTimeText  { get => _sessionTimeText;  private set => SetProperty(ref _sessionTimeText,  value); }
    public string UserLevelText    { get => _userLevelText;    private set => SetProperty(ref _userLevelText,    value); }
    public string XpProgressText   { get => _xpProgressText;   private set => SetProperty(ref _xpProgressText,   value); }
    public double XpProgress       { get => _xpProgress;       private set => SetProperty(ref _xpProgress,       value); }
    public string MouseSummaryText { get => _mouseSummaryText; private set => SetProperty(ref _mouseSummaryText, value); }

    // Streak properties
    public int CurrentStreak
    {
        get => _currentStreak;
        private set
        {
            if (SetProperty(ref _currentStreak, value))
            {
                OnPropertyChanged(nameof(StreakBadgeText));
                OnPropertyChanged(nameof(HasStreak));
                OnPropertyChanged(nameof(StreakTooltip));
            }
        }
    }

    public string StreakBadgeText => CurrentStreak > 0 ? $"🔥 {CurrentStreak}" : string.Empty;
    public bool HasStreak => CurrentStreak > 0 && ShowStreak;
    public string StreakTooltip => LocalizationService.IsRussian
        ? $"Текущая серия активности: {CurrentStreak} дн."
        : $"Current activity streak: {CurrentStreak} d.";

    // Active App Limit properties
    public bool HasAppLimit          => _hasAppLimit && ShowLimit;
    public bool DoesNotHaveAppLimit  => !HasAppLimit;
    public bool IsLimitExceeded      => _isLimitExceeded;
    public bool IsLimitWarning    => _isLimitWarning;
    public string LimitRemainingText => _limitRemainingText;
    public string LimitStatusText    => _limitStatusText;
    public double LimitProgress      => _limitProgress;

    public string LimitBadgeColor
    {
        get
        {
            if (_isLimitExceeded) return "#EF4444";
            if (_isLimitWarning)  return "#F59E0B";
            return "#10B981";
        }
    }

    // Display mode: 0 = Standard, 1 = Compact, 2 = Micro
    public int WidgetDisplayMode
    {
        get => _settings.WidgetDisplayMode;
        set => SetWidgetDisplayMode(value);
    }

    public bool IsStandardMode   => WidgetDisplayMode == 0;
    public bool IsCompactMode    => WidgetDisplayMode == 1;
    public bool IsNotCompactMode => WidgetDisplayMode == 0;
    public bool IsMicroMode      => WidgetDisplayMode == 2;
    public double WidgetContainerWidth => IsMicroMode ? double.NaN : 224;

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

    public bool ShowStreak
    {
        get => _settings.WidgetShowStreak;
        set
        {
            if (_settings.WidgetShowStreak != value)
            {
                _settings.WidgetShowStreak = value;
                OnPropertyChanged(nameof(ShowStreak));
                OnPropertyChanged(nameof(HasStreak));
            }
        }
    }

    public bool ShowLimit
    {
        get => _settings.WidgetShowLimit;
        set
        {
            if (_settings.WidgetShowLimit != value)
            {
                _settings.WidgetShowLimit = value;
                OnPropertyChanged(nameof(ShowLimit));
                OnPropertyChanged(nameof(HasAppLimit));
                OnPropertyChanged(nameof(DoesNotHaveAppLimit));
            }
        }
    }

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

    public string WidgetToggleLabel => IsWidgetEnabled 
        ? (System.Windows.Application.Current?.TryFindResource("Widget_BtnDisable") as string ?? "Выключить виджет") 
        : (System.Windows.Application.Current?.TryFindResource("Widget_BtnEnable") as string ?? "Включить виджет");

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

    public event EventHandler<bool>?   WidgetVisibilityChanged;
    public event EventHandler<double>? WidgetOpacityChanged;
    public event EventHandler<bool>?   CompactModeChanged;
    public event EventHandler<int>?    DisplayModeChanged;
    public event EventHandler<bool>?   ClickThroughChanged;
    public event EventHandler<bool>?   TopmostChanged;
    public event EventHandler?         ResetPositionRequested;

    public ICommand ToggleWidgetCommand   { get; }
    public ICommand ResetPositionCommand  { get; }
    public ICommand ToggleCompactCommand  { get; }
    public ICommand SetDisplayModeCommand { get; }
    public ICommand CycleModeCommand      { get; }

    private long _todayActiveSeconds;
    private long _continuousSessionSeconds;
    private int  _dbRefreshCounter;

    public DesktopWidgetViewModel(
        ActivityTracker tracker,
        ActivityRepository activityRepo,
        SettingsService settings,
        LimitEnforcerService limitEnforcer,
        DailyQuestEngine questEngine,
        LocalizationService localization)
    {
        _tracker       = tracker;
        _activityRepo  = activityRepo;
        _settings      = settings;
        _limitEnforcer = limitEnforcer;
        _questEngine   = questEngine;
        _localization  = localization;

        ToggleWidgetCommand   = new RelayCommand(() => IsWidgetEnabled = !IsWidgetEnabled);
        ResetPositionCommand  = new RelayCommand(() => ResetPositionRequested?.Invoke(this, EventArgs.Empty));
        ToggleCompactCommand  = new RelayCommand(ToggleCompact);
        CycleModeCommand      = new RelayCommand(CycleDisplayMode);
        SetDisplayModeCommand = new RelayCommand<int>(SetWidgetDisplayMode);

        _tracker.StateChanged += OnTrackerStateChanged;
        _localization.LanguageChanged += (_, _) =>
        {
            UserLevelText = LocalizationService.IsRussian ? $"Ур. {_currentUserLevel}" : $"Lvl {_currentUserLevel}";
            TodayTimeText = Fmt(_todayActiveSeconds);
            SessionTimeText = Fmt(_continuousSessionSeconds);
            var (clicks, dist) = _tracker.GetTodayMouseMetrics();
            string clicksUnit = LocalizationService.IsRussian ? "кл" : "clicks";
            MouseSummaryText = $"{DashboardViewModel.FormatClicks(clicks)} {clicksUnit} · {DashboardViewModel.FormatDistance(dist)}";
            OnPropertyChanged(nameof(WidgetToggleLabel));
            OnPropertyChanged(nameof(StreakTooltip));
            OnPropertyChanged(nameof(LimitStatusText));
            _ = UpdateLifetimeAndLevelAsync();
        };
    }

    public void ToggleCompact()
    {
        CycleDisplayMode();
    }

    public void SetWidgetDisplayMode(int mode)
    {
        mode = Math.Clamp(mode, 0, 2);
        if (_settings.WidgetDisplayMode != mode)
        {
            _settings.WidgetDisplayMode = mode;
            OnPropertyChanged(nameof(WidgetDisplayMode));
            OnPropertyChanged(nameof(IsStandardMode));
            OnPropertyChanged(nameof(IsCompactMode));
            OnPropertyChanged(nameof(IsNotCompactMode));
            OnPropertyChanged(nameof(IsMicroMode));
            OnPropertyChanged(nameof(WidgetContainerWidth));
            CompactModeChanged?.Invoke(this, mode == 1);
            DisplayModeChanged?.Invoke(this, mode);
        }
    }

    public void CycleDisplayMode()
    {
        int next = (WidgetDisplayMode + 1) % 3;
        SetWidgetDisplayMode(next);
    }

    public void NotifySettingsChanged()
    {
        OnPropertyChanged(nameof(ShowApp));
        OnPropertyChanged(nameof(ShowTime));
        OnPropertyChanged(nameof(ShowSession));
        OnPropertyChanged(nameof(ShowLevel));
        OnPropertyChanged(nameof(ShowMouse));
        OnPropertyChanged(nameof(ShowStreak));
        OnPropertyChanged(nameof(ShowLimit));
    }

    public async Task InitializeAsync()
    {
        try
        {
            var (active, _) = await _activityRepo.GetTotalsAsync(DateTime.Today, DateTime.Today.AddDays(1));
            _todayActiveSeconds = active;
            TodayTimeText = Fmt(active);

            await UpdateLifetimeAndLevelAsync();
            await UpdateStreakAsync();

            var (clicks, dist) = _tracker.GetTodayMouseMetrics();
            string clicksUnit = LocalizationService.IsRussian ? "кл" : "clicks";
            MouseSummaryText = $"{DashboardViewModel.FormatClicks(clicks)} {clicksUnit} · {DashboardViewModel.FormatDistance(dist)}";
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
                CurrentAppName = string.IsNullOrWhiteSpace(e.AppName) 
                    ? (LocalizationService.IsRussian ? "Активность" : "Activity") 
                    : e.AppName;

                // Update active app limit status
                if (e.AppId > 0)
                {
                    var status = _limitEnforcer.GetLimitStatus(e.AppId);
                    if (status.HasValue)
                    {
                        var (limitSec, usedSec, isExceeded) = status.Value;
                        int rem = Math.Max(0, limitSec - usedSec);
                        _hasAppLimit = true;
                        _isLimitExceeded = isExceeded;
                        _isLimitWarning = !isExceeded && rem <= 300;
                        _limitRemainingText = LocalizationService.FormatDuration(rem);
                        _limitProgress = limitSec > 0 ? Math.Clamp((double)usedSec / limitSec * 100.0, 0, 100) : 0;
                        _limitStatusText = isExceeded
                            ? (LocalizationService.IsRussian ? "Лимит исчерпан!" : "Limit exceeded!")
                            : string.Format(_localization.GetString("Widget_LimitRemaining"), _limitRemainingText);
                    }
                    else
                    {
                        _hasAppLimit = false;
                        _isLimitExceeded = false;
                        _isLimitWarning = false;
                        _limitRemainingText = string.Empty;
                        _limitStatusText = string.Empty;
                    }
                }
                else
                {
                    _hasAppLimit = false;
                }
            }
            else
            {
                _continuousSessionSeconds = 0;
                SessionTimeText = LocalizationService.FormatDuration(0);
                CurrentAppName = LocalizationService.IsRussian ? "💤 AFK / Бездействие" : "💤 AFK / Idle";
                _hasAppLimit = false;
            }

            OnPropertyChanged(nameof(HasAppLimit));
            OnPropertyChanged(nameof(DoesNotHaveAppLimit));
            OnPropertyChanged(nameof(IsLimitExceeded));
            OnPropertyChanged(nameof(IsLimitWarning));
            OnPropertyChanged(nameof(LimitRemainingText));
            OnPropertyChanged(nameof(LimitStatusText));
            OnPropertyChanged(nameof(LimitProgress));
            OnPropertyChanged(nameof(LimitBadgeColor));

            var (clicks, dist) = _tracker.GetTodayMouseMetrics();
            string clicksUnit = LocalizationService.IsRussian ? "кл" : "clicks";
            MouseSummaryText = $"{DashboardViewModel.FormatClicks(clicks)} {clicksUnit} · {DashboardViewModel.FormatDistance(dist)}";

            if (++_dbRefreshCounter >= 60)
            {
                _dbRefreshCounter = 0;
                _ = UpdateLifetimeAndLevelAsync();
                _ = UpdateStreakAsync();
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

            _currentUserLevel = level;
            UserLevelText = LocalizationService.IsRussian ? $"Ур. {level}" : $"Lvl {level}";
            XpProgressText = $"{curLvlXp} / {xpPerLevel} XP";
            XpProgress = Math.Clamp((double)curLvlXp / xpPerLevel * 100.0, 0, 100);
        }
        catch { }
    }

    private async Task UpdateStreakAsync()
    {
        try
        {
            var streak = await _questEngine.CalculateStreakAsync();
            CurrentStreak = streak.CurrentStreak;
        }
        catch { }
    }

    private static string Fmt(long s) =>
        LocalizationService.FormatDurationFull(s);
}
