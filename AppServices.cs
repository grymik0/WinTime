using WinTime.Core;
using WinTime.Data;
using WinTime.Services;
using WinTime.ViewModels;

namespace WinTime;

/// <summary>
/// Service locator and dependency provider for WinTime services and viewmodels.
/// </summary>
internal static class AppServices
{
    public static SettingsService         Settings      { get; private set; } = null!;
    public static DatabaseService         Database      { get; private set; } = null!;
    public static ApplicationRepository   AppRepo       { get; private set; } = null!;
    public static ActivityRepository      ActivityRepo  { get; private set; } = null!;
    public static UptimeRepository        UptimeRepo    { get; private set; } = null!;
    public static IconService             IconService   { get; private set; } = null!;
    public static ExportService           ExportService { get; private set; } = null!;
    public static ThemeService            ThemeService  { get; private set; } = null!;
    public static LocalizationService     Localization  { get; private set; } = null!;
    public static InMemoryBuffer          Buffer        { get; private set; } = null!;
    public static ActivityTracker         Tracker       { get; private set; } = null!;

    public static DashboardViewModel          DashboardVm          { get; private set; } = null!;
    public static ProcessesViewModel          ProcessesVm          { get; private set; } = null!;
    public static ApplicationsViewModel       ApplicationsVm       { get; private set; } = null!;
    public static ProfileViewModel            ProfileVm            { get; private set; } = null!;
    public static DesktopWidgetViewModel      DesktopWidgetVm      { get; private set; } = null!;
    public static ThemeCustomizationViewModel ThemeVm              { get; private set; } = null!;
    public static SettingsViewModel           SettingsVm           { get; private set; } = null!;
    public static MainWindowViewModel         MainWindowVm         { get; private set; } = null!;

    public static void Initialize(SettingsService settings)
    {
        Settings     = settings;
        Localization = new LocalizationService(settings);
        Localization.Initialize();

        ThemeService = new ThemeService(settings);
        ThemeService.Initialize();

        Database     = new DatabaseService();
        Database.Initialize(settings.DatabasePath!);

        AppRepo      = new ApplicationRepository(Database);
        ActivityRepo = new ActivityRepository(Database);
        UptimeRepo   = new UptimeRepository(Database);
        IconService  = new IconService();
        ExportService = new ExportService(ActivityRepo, AppRepo);

        Buffer  = new InMemoryBuffer();
        Tracker = new ActivityTracker(AppRepo, ActivityRepo, UptimeRepo, Buffer, Settings);

        DashboardVm          = new DashboardViewModel(ActivityRepo, IconService, Tracker);
        ProcessesVm          = new ProcessesViewModel(UptimeRepo, ActivityRepo, Tracker);
        ApplicationsVm       = new ApplicationsViewModel(AppRepo, ActivityRepo);
        ProfileVm            = new ProfileViewModel(ActivityRepo, Tracker);
        DesktopWidgetVm      = new DesktopWidgetViewModel(Tracker, ActivityRepo, Settings);
        ThemeVm              = new ThemeCustomizationViewModel(ThemeService, Localization);
        SettingsVm           = new SettingsViewModel(Settings, ActivityRepo, ExportService);
        MainWindowVm         = new MainWindowViewModel(DashboardVm, ProcessesVm, ApplicationsVm, ProfileVm, DesktopWidgetVm, ThemeVm, SettingsVm, Localization);
    }

    public static async Task ShutdownAsync()
    {
        if (Tracker is not null)
            await Tracker.StopAsync();

        Database?.Dispose();
    }
}
