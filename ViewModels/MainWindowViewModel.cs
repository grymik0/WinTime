using System.Windows.Input;
using WinTime.Services;

namespace WinTime.ViewModels;

/// <summary>
/// Main window ViewModel managing primary navigation and tracker status.
/// </summary>
public sealed class MainWindowViewModel : BaseViewModel
{
    private readonly DashboardViewModel          _dashboard;
    private readonly ProcessesViewModel          _processes;
    private readonly ApplicationsViewModel       _applications;
    private readonly GoalsViewModel              _goals;
    private readonly ProjectsViewModel           _projects;
    private readonly ProfileViewModel            _profile;
    private readonly DesktopWidgetViewModel      _widget;
    private readonly ThemeCustomizationViewModel _theme;
    private readonly SettingsViewModel           _settings;
    private readonly LocalizationService         _localization;

    private object? _currentView;
    private bool    _isTracking = true;

    public object? CurrentView
    {
        get => _currentView;
        private set
        {
            if (SetProperty(ref _currentView, value))
            {
                OnPropertyChanged(nameof(IsDashboardActive));
                OnPropertyChanged(nameof(IsProcessesActive));
                OnPropertyChanged(nameof(IsApplicationsActive));
                OnPropertyChanged(nameof(IsGoalsActive));
                OnPropertyChanged(nameof(IsProjectsActive));
                OnPropertyChanged(nameof(IsProfileActive));
                OnPropertyChanged(nameof(IsWidgetActive));
                OnPropertyChanged(nameof(IsThemeActive));
                OnPropertyChanged(nameof(IsSettingsActive));
            }
        }
    }

    public bool IsDashboardActive    => CurrentView == _dashboard;
    public bool IsProcessesActive    => CurrentView == _processes;
    public bool IsApplicationsActive => CurrentView == _applications;
    public bool IsGoalsActive        => CurrentView == _goals;
    public bool IsProjectsActive     => CurrentView == _projects;
    public bool IsProfileActive      => CurrentView == _profile;
    public bool IsWidgetActive       => CurrentView == _widget;
    public bool IsThemeActive        => CurrentView == _theme;
    public bool IsSettingsActive     => CurrentView == _settings;

    public bool IsTracking
    {
        get => _isTracking;
        private set { SetProperty(ref _isTracking, value); OnPropertyChanged(nameof(TrackingLabel)); }
    }

    public string TrackingLabel => _isTracking 
        ? _localization.GetString("Nav_PauseTracking") 
        : _localization.GetString("Nav_ResumeTracking");

    public ICommand NavigateDashboardCommand    { get; }
    public ICommand NavigateProcessesCommand    { get; }
    public ICommand NavigateApplicationsCommand { get; }
    public ICommand NavigateGoalsCommand        { get; }
    public ICommand NavigateProjectsCommand     { get; }
    public ICommand NavigateProfileCommand      { get; }
    public ICommand NavigateWidgetCommand       { get; }
    public ICommand NavigateThemeCommand        { get; }
    public ICommand NavigateSettingsCommand     { get; }
    public ICommand ToggleTrackingCommand       { get; }

    public MainWindowViewModel(
        DashboardViewModel          dashboard,
        ProcessesViewModel          processes,
        ApplicationsViewModel       applications,
        GoalsViewModel              goals,
        ProjectsViewModel           projects,
        ProfileViewModel            profile,
        DesktopWidgetViewModel      widget,
        ThemeCustomizationViewModel theme,
        SettingsViewModel           settings,
        LocalizationService         localization)
    {
        _dashboard    = dashboard;
        _processes    = processes;
        _applications = applications;
        _goals        = goals;
        _projects     = projects;
        _profile      = profile;
        _widget       = widget;
        _theme        = theme;
        _settings     = settings;
        _localization = localization;

        _localization.LanguageChanged += (_, _) => OnPropertyChanged(nameof(TrackingLabel));

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

        NavigateGoalsCommand = new RelayCommand(() =>
        {
            CurrentView = _goals;
            _ = _goals.LoadAsync();
        });

        NavigateProjectsCommand = new RelayCommand(() =>
        {
            CurrentView = _projects;
            _ = _projects.LoadDataAsync();
        });

        NavigateProfileCommand = new RelayCommand(() =>
        {
            CurrentView = _profile;
            _ = _profile.LoadAsync();
        });

        NavigateWidgetCommand = new RelayCommand(() =>
        {
            CurrentView = _widget;
        });

        NavigateThemeCommand = new RelayCommand(() =>
        {
            CurrentView = _theme;
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

        // Open dashboard on startup
        CurrentView = _dashboard;
        _ = _dashboard.LoadDataAsync();
    }
}

