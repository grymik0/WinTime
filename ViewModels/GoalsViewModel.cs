using System.Collections.ObjectModel;
using System.Windows.Input;
using WinTime.Core;
using WinTime.Data;
using WinTime.Models;
using WinTime.Services;

namespace WinTime.ViewModels;

public sealed class GoalsViewModel : BaseViewModel
{
    private readonly GoalRepository        _goalRepo;
    private readonly ApplicationRepository _appRepo;
    private readonly ActivityRepository    _activityRepo;
    private readonly LimitEnforcerService  _enforcer;
    private readonly LocalizationService  _localization;

    private ObservableCollection<AppLimitItem> _limits = [];
    private List<AppModel> _availableApps = [];
    private AppModel? _selectedAppForLimit;

    private bool _isDialogOpen;
    private int _dialogHours = 1;
    private int _dialogMinutes = 30;
    private LimitActionType _dialogActionType = LimitActionType.Notify;

    public ObservableCollection<AppLimitItem> Limits
    {
        get => _limits;
        private set => SetProperty(ref _limits, value);
    }

    public List<AppModel> AvailableApps
    {
        get => _availableApps;
        private set => SetProperty(ref _availableApps, value);
    }

    public AppModel? SelectedAppForLimit
    {
        get => _selectedAppForLimit;
        set => SetProperty(ref _selectedAppForLimit, value);
    }

    public bool IsDialogOpen
    {
        get => _isDialogOpen;
        set => SetProperty(ref _isDialogOpen, value);
    }

    public int DialogHours
    {
        get => _dialogHours;
        set => SetProperty(ref _dialogHours, Math.Clamp(value, 0, 23));
    }

    public int DialogMinutes
    {
        get => _dialogMinutes;
        set => SetProperty(ref _dialogMinutes, Math.Clamp(value, 0, 59));
    }

    public LimitActionType DialogActionType
    {
        get => _dialogActionType;
        set => SetProperty(ref _dialogActionType, value);
    }

    public bool HasNoLimits => Limits.Count == 0;

    public ICommand OpenAddDialogCommand   { get; }
    public ICommand CloseDialogCommand     { get; }
    public ICommand SaveLimitCommand       { get; }
    public ICommand DeleteLimitCommand     { get; }
    public ICommand ToggleLimitCommand     { get; }

    public GoalsViewModel(
        GoalRepository goalRepo,
        ApplicationRepository appRepo,
        ActivityRepository activityRepo,
        LimitEnforcerService enforcer,
        LocalizationService localization)
    {
        _goalRepo     = goalRepo;
        _appRepo      = appRepo;
        _activityRepo = activityRepo;
        _enforcer     = enforcer;
        _localization = localization;

        _localization.LanguageChanged += (_, _) =>
        {
            foreach (var item in Limits)
                item.RefreshFormatted();
        };

        OpenAddDialogCommand = new RelayCommand(async () => await PrepareAndOpenDialogAsync());
        CloseDialogCommand   = new RelayCommand(() => IsDialogOpen = false);
        SaveLimitCommand     = new RelayCommand(async () => await SaveLimitAsync());
        DeleteLimitCommand   = new RelayCommand<AppLimitItem>(async item =>
        {
            if (item is null) return;
            await DeleteLimitAsync(item);
        });
        ToggleLimitCommand   = new RelayCommand<AppLimitItem>(async item =>
        {
            if (item is null) return;
            await ToggleLimitAsync(item);
        });
    }

    public async Task LoadAsync()
    {
        try
        {
            var list = await _goalRepo.GetAllLimitsAsync();
            var today = DateTime.Today;
            var appStats = await _activityRepo.GetTopAppsAsync(today, today.AddDays(1));
            var statsDict = appStats.ToDictionary(s => s.AppId, s => (int)s.TotalSeconds);

            foreach (var item in list)
            {
                if (statsDict.TryGetValue(item.AppId, out int used))
                    item.TodayUsedSeconds = used;
            }

            Limits = new ObservableCollection<AppLimitItem>(list);
            OnPropertyChanged(nameof(HasNoLimits));
        }
        catch { }
    }

    private async Task PrepareAndOpenDialogAsync()
    {
        try
        {
            var allApps = await _appRepo.GetAllAsync();
            var existingAppIds = Limits.Select(l => l.AppId).ToHashSet();
            AvailableApps = allApps.Where(a => !existingAppIds.Contains(a.Id)).ToList();
            SelectedAppForLimit = AvailableApps.FirstOrDefault();
            DialogHours = 1;
            DialogMinutes = 30;
            DialogActionType = LimitActionType.Notify;
            IsDialogOpen = true;
        }
        catch { }
    }

    private async Task SaveLimitAsync()
    {
        if (SelectedAppForLimit is null) return;
        int totalSec = (DialogHours * 3600) + (DialogMinutes * 60);
        if (totalSec <= 0) totalSec = 60; // minimum 1 minute

        await _goalRepo.SetLimitAsync(SelectedAppForLimit.Id, totalSec, DialogActionType, isEnabled: true);
        await _enforcer.ReloadLimitsAsync();
        IsDialogOpen = false;
        await LoadAsync();
    }

    private async Task DeleteLimitAsync(AppLimitItem item)
    {
        await _goalRepo.DeleteLimitAsync(item.Id);
        await _enforcer.ReloadLimitsAsync();
        await LoadAsync();
    }

    private async Task ToggleLimitAsync(AppLimitItem item)
    {
        await _goalRepo.ToggleLimitAsync(item.Id, item.IsEnabled);
        await _enforcer.ReloadLimitsAsync();
    }
}
