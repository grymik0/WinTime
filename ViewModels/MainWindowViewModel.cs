using System.Windows.Input;

namespace WinTime.ViewModels;

/// <summary>
/// ViewModel главного окна:
/// — управляет навигацией (CurrentView → DataTemplate → нужный UserControl)
/// — отражает статус трекера (пауза / активен)
/// </summary>
public sealed class MainWindowViewModel : BaseViewModel
{
    private readonly DashboardViewModel    _dashboard;
    private readonly ProcessesViewModel    _processes;
    private readonly ApplicationsViewModel _applications;
    private readonly ProfileViewModel     _profile;
    private readonly SettingsViewModel     _settings;

    private object? _currentView;
    private bool    _isTracking = true;

    public object? CurrentView
    {
        get => _currentView;
        private set => SetProperty(ref _currentView, value);
    }

    public bool IsTracking
    {
        get => _isTracking;
        private set { SetProperty(ref _isTracking, value); OnPropertyChanged(nameof(TrackingLabel)); }
    }

    /// <summary>Текст кнопки «Пауза / Возобновить» в боковой панели.</summary>
    public string TrackingLabel => _isTracking ? "⏸  Приостановить" : "▶  Возобновить";

    // Commands

    public ICommand NavigateDashboardCommand    { get; }
    public ICommand NavigateProcessesCommand    { get; }
    public ICommand NavigateApplicationsCommand { get; }
    public ICommand NavigateProfileCommand      { get; }
    public ICommand NavigateSettingsCommand     { get; }
    public ICommand ToggleTrackingCommand       { get; }

    // Constructor

    public MainWindowViewModel(
        DashboardViewModel    dashboard,
        ProcessesViewModel    processes,
        ApplicationsViewModel applications,
        ProfileViewModel      profile,
        SettingsViewModel     settings)
    {
        _dashboard    = dashboard;
        _processes    = processes;
        _applications = applications;
        _profile      = profile;
        _settings     = settings;

        NavigateDashboardCommand = new RelayCommand(() =>
        {
            CurrentView = _dashboard;
            _ = _dashboard.LoadDataAsync();
        });

        NavigateProcessesCommand = new RelayCommand(() =>
        {
            CurrentView = _processes;
            _ = _processes.LoadAsync();
        });

        NavigateApplicationsCommand = new RelayCommand(() =>
        {
            CurrentView = _applications;
            _ = _applications.LoadAsync();
        });

        NavigateProfileCommand = new RelayCommand(() =>
        {
            CurrentView = _profile;
            _ = _profile.LoadAsync();
        });

        NavigateSettingsCommand = new RelayCommand(() =>
        {
            CurrentView = _settings;
        });

        ToggleTrackingCommand = new RelayCommand(() =>
        {
            IsTracking = !IsTracking;
            AppServices.Tracker.IsPaused = !IsTracking;
        });

        // Открываем дашборд при старте
        CurrentView = _dashboard;
        _ = _dashboard.LoadDataAsync();
    }
}

