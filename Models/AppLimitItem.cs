namespace WinTime.Models;

public enum LimitActionType
{
    Notify = 0,
    CloseProcess = 1
}

public sealed class AppLimitItem : ViewModels.BaseViewModel
{
    private int _id;
    private int _appId;
    private string _processName = string.Empty;
    private string _displayName = string.Empty;
    private string? _iconBlob;
    private int _maxDailySeconds;
    private LimitActionType _actionType;
    private bool _isEnabled = true;
    private int _todayUsedSeconds;

    public int Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public int AppId
    {
        get => _appId;
        set => SetProperty(ref _appId, value);
    }

    public string ProcessName
    {
        get => _processName;
        set => SetProperty(ref _processName, value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public string? IconBlob
    {
        get => _iconBlob;
        set => SetProperty(ref _iconBlob, value);
    }

    public int MaxDailySeconds
    {
        get => _maxDailySeconds;
        set
        {
            if (SetProperty(ref _maxDailySeconds, value))
            {
                OnPropertyChanged(nameof(FormattedLimit));
                OnPropertyChanged(nameof(ProgressPercent));
                OnPropertyChanged(nameof(IsExceeded));
            }
        }
    }

    public LimitActionType ActionType
    {
        get => _actionType;
        set
        {
            if (SetProperty(ref _actionType, value))
            {
                OnPropertyChanged(nameof(ActionTypeTitle));
            }
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public int TodayUsedSeconds
    {
        get => _todayUsedSeconds;
        set
        {
            if (SetProperty(ref _todayUsedSeconds, value))
            {
                OnPropertyChanged(nameof(FormattedUsed));
                OnPropertyChanged(nameof(ProgressPercent));
                OnPropertyChanged(nameof(IsExceeded));
            }
        }
    }

    public string FriendlyName =>
        !string.IsNullOrWhiteSpace(DisplayName) ? DisplayName : ProcessName;

    public string FormattedLimit =>
        Services.LocalizationService.FormatDuration(MaxDailySeconds);

    public string FormattedUsed =>
        Services.LocalizationService.FormatDuration(TodayUsedSeconds);

    public double ProgressPercent =>
        MaxDailySeconds > 0
            ? Math.Clamp(Math.Round((double)TodayUsedSeconds / MaxDailySeconds * 100.0, 1), 0, 100)
            : 0;

    public bool IsExceeded => IsEnabled && MaxDailySeconds > 0 && TodayUsedSeconds >= MaxDailySeconds;

    public string ActionTypeTitle => ActionType switch
    {
        LimitActionType.CloseProcess => Services.LocalizationService.IsRussian
            ? "Закрытие"
            : "Close app",
        _ => Services.LocalizationService.IsRussian
            ? "Уведомления"
            : "Notifications"
    };

    public void RefreshFormatted()
    {
        OnPropertyChanged(nameof(FormattedLimit));
        OnPropertyChanged(nameof(FormattedUsed));
        OnPropertyChanged(nameof(ActionTypeTitle));
    }
}

