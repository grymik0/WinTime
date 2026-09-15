using WinTime.Core;
using WinTime.Data;
using WinTime.Services;
using WinTime.ViewModels;

namespace WinTime;

/// <summary>
/// Service Locator — единственное место создания и хранения всех сервисов.
/// Инициализируется в App.OnStartup() после выбора пути к БД.
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
    public static InMemoryBuffer          Buffer        { get; private set; } = null!;
    public static ActivityTracker         Tracker       { get; private set; } = null!;

    // ViewModels 
    public static DashboardViewModel      DashboardVm     { get; private set; } = null!;
    public static ProcessesViewModel      ProcessesVm     { get; private set; } = null!;
    public static ApplicationsViewModel   ApplicationsVm  { get; private set; } = null!;
    public static SettingsViewModel       SettingsVm      { get; private set; } = null!;
    public static MainWindowViewModel     MainWindowVm    { get; private set; } = null!;

    // Initialization

    public static void Initialize(SettingsService settings)
    {
        Settings     = settings;
        Database     = new DatabaseService();
        Database.Initialize(settings.DatabasePath!);

        AppRepo      = new ApplicationRepository(Database);
        ActivityRepo = new ActivityRepository(Database);
        UptimeRepo   = new UptimeRepository(Database);
        IconService  = new IconService();
        ExportService = new ExportService(ActivityRepo, AppRepo);

        Buffer  = new InMemoryBuffer();
        Tracker = new ActivityTracker(AppRepo, ActivityRepo, UptimeRepo, Buffer, Settings);

        // ViewModels
        DashboardVm    = new DashboardViewModel(ActivityRepo, IconService, Tracker);
        ProcessesVm    = new ProcessesViewModel(UptimeRepo, ActivityRepo, Tracker);
        ApplicationsVm = new ApplicationsViewModel(AppRepo, ActivityRepo);
        SettingsVm     = new SettingsViewModel(Settings, ActivityRepo, ExportService);
        MainWindowVm   = new MainWindowViewModel(DashboardVm, ProcessesVm, ApplicationsVm, SettingsVm);
    }

    public static async Task ShutdownAsync()
    {
        if (Tracker is not null)
            await Tracker.StopAsync();

        Database?.Dispose();
    }
}

