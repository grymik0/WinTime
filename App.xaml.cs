using System.Drawing;
using System.Threading;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using WinTime.Core;
using WinTime.Services;
using WinTime.Views;

namespace WinTime;

/// <summary>
/// Application entry point and lifecycle manager.
/// </summary>
public partial class App : Application
{
    private TaskbarIcon?         _trayIcon;
    private MainWindow?          _mainWindow;
    private DesktopWidgetWindow? _widgetWindow;
    private bool                 _isExiting;
    private Mutex?               _singleInstanceMutex;

    public bool IsExiting => _isExiting;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            SystemStartupManager.UnblockFile(Environment.ProcessPath);
        }
        catch { }

        _singleInstanceMutex = new Mutex(true, "WinTime_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "WinTime is already running.\nCheck your system tray icon.",
                "WinTime", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        DispatcherUnhandledException += (_, ex) =>
        {
            ex.Handled = true;
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{ex.Exception.Message}",
                "WinTime Error", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        var settings = new SettingsService();
        settings.Load();

        var localization = new LocalizationService(settings);
        localization.Initialize();

        var theme = new ThemeService(settings);
        theme.Initialize();

        if (!settings.IsDatabaseConfigured)
        {
            var dlg = new FirstRunDialog(settings, localization);
            if (dlg.ShowDialog() != true)
            {
                Shutdown();
                return;
            }
            settings.DatabasePath = dlg.SelectedDbPath;
        }

        try
        {
            AppServices.Initialize(settings);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to initialize database:\n{settings.DatabasePath}\n\n{ex.Message}",
                "WinTime Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        AppServices.Tracker.Start();

        SystemStartupManager.SyncCurrentExePath(settings.LaunchOnStartup);

        SetupTrayIcon();

        _mainWindow = new MainWindow();
        MainWindow = _mainWindow;
        _mainWindow.Show();

        InitDesktopWidget(settings);

        _ = CheckWeeklyRecapPromptAsync(settings);
    }

    private async Task CheckWeeklyRecapPromptAsync(SettingsService settings)
    {
        try
        {
            DateTime today = DateTime.Today;
            int diff = (int)today.DayOfWeek - (int)DayOfWeek.Monday;
            if (diff < 0) diff += 7;
            DateTime weekMonday = today.AddDays(-diff);
            string currentWeekKey = weekMonday.ToString("yyyy-MM-dd");

            if (settings.LastWeeklyRecapNotifiedWeek == currentWeekKey)
                return;

            // Allow the main window to settle
            await Task.Delay(1500);

            // Check if there is meaningful activity in the past 7 days (>= 30 mins)
            var (activeSec, _) = await AppServices.ActivityRepo.GetTotalsAsync(today.AddDays(-7), today.AddDays(1));
            if (activeSec < 1800)
                return;

            // Record this week so the prompt is never shown again this week
            settings.LastWeeklyRecapNotifiedWeek = currentWeekKey;

            var dlg = new WeeklyRecapPromptDialog
            {
                Owner = _mainWindow
            };

            dlg.ShowDialog();

            if (dlg.UserWantsToViewRecap)
            {
                var vm = new ViewModels.WeeklyRecapViewModel(AppServices.ActivityRepo, AppServices.Localization);
                await vm.LoadAsync();
                var recapWnd = new WeeklyRecapWindow(vm)
                {
                    Owner = _mainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                recapWnd.ShowDialog();
            }
        }
        catch { }
    }

    private void InitDesktopWidget(SettingsService settings)
    {
        _widgetWindow = new DesktopWidgetWindow(AppServices.DesktopWidgetVm, settings);
        _ = AppServices.DesktopWidgetVm.InitializeAsync();

        if (settings.ShowWidget)
        {
            _widgetWindow.Show();
        }

        AppServices.DesktopWidgetVm.WidgetVisibilityChanged += (_, isVisible) =>
        {
            if (isVisible)
            {
                _widgetWindow.Show();
                _widgetWindow.Activate();
            }
            else
            {
                _widgetWindow.Hide();
            }
        };

        AppServices.DesktopWidgetVm.WidgetOpacityChanged += (_, _) =>
        {
            _widgetWindow.UpdateOpacity();
        };

        AppServices.DesktopWidgetVm.ClickThroughChanged += (_, enable) =>
        {
            _widgetWindow.UpdateClickThrough(enable);
        };

        AppServices.DesktopWidgetVm.TopmostChanged += (_, topmost) =>
        {
            _widgetWindow.UpdateTopmost(topmost);
        };

        AppServices.DesktopWidgetVm.ResetPositionRequested += (_, _) =>
        {
            _widgetWindow.ResetPosition();
        };
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _isExiting = true;
        _widgetWindow?.Close();
        _trayIcon?.Dispose();

        await AppServices.ShutdownAsync();

        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();

        base.OnExit(e);
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "WinTime",
            Icon        = CreateTrayIcon()
        };

        _trayIcon.TrayMouseDoubleClick += (_, _) => ShowMainWindow();

        var menu = new System.Windows.Controls.ContextMenu();

        var itemOpen = new System.Windows.Controls.MenuItem { Header = "📊  WinTime" };
        itemOpen.Click += (_, _) => ShowMainWindow();

        var itemWidget = new System.Windows.Controls.MenuItem { Header = "📌  Desktop Widget" };
        itemWidget.Click += (_, _) =>
        {
            AppServices.DesktopWidgetVm.IsWidgetEnabled = !AppServices.DesktopWidgetVm.IsWidgetEnabled;
        };

        var itemPause = new System.Windows.Controls.MenuItem { Header = "⏸  Pause Tracking" };
        itemPause.Click += (_, _) =>
        {
            AppServices.Tracker.IsPaused = !AppServices.Tracker.IsPaused;
            itemPause.Header = AppServices.Tracker.IsPaused
                ? "▶  Resume Tracking"
                : "⏸  Pause Tracking";
            _trayIcon.ToolTipText = AppServices.Tracker.IsPaused
                ? "WinTime (Paused)"
                : "WinTime";
        };

        var itemSettings = new System.Windows.Controls.MenuItem { Header = "⚙  Settings" };
        itemSettings.Click += (_, _) =>
        {
            ShowMainWindow();
            AppServices.MainWindowVm.NavigateSettingsCommand.Execute(null);
        };

        var itemExit = new System.Windows.Controls.MenuItem { Header = "✕  Exit" };
        itemExit.Click += (_, _) => ExitApp();

        menu.Items.Add(itemOpen);
        menu.Items.Add(new System.Windows.Controls.Separator());
        menu.Items.Add(itemWidget);
        menu.Items.Add(itemPause);
        menu.Items.Add(itemSettings);
        menu.Items.Add(new System.Windows.Controls.Separator());
        menu.Items.Add(itemExit);

        _trayIcon.ContextMenu = menu;
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null) return;

        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private void ExitApp()
    {
        _isExiting = true;
        Shutdown();
    }

    /// <summary>
    /// Loads the tray icon from application resources, or generates a fallback icon.
    /// </summary>
    private static Icon CreateTrayIcon()
    {
        try
        {
            var stream = GetResourceStream(
                new Uri("pack://application:,,,/Assets/icon.ico"))?.Stream;
            if (stream is not null)
            {
                return new Icon(stream, new System.Drawing.Size(32, 32));
            }
        }
        catch {  }

        using var fb  = new System.Drawing.Bitmap(32, 32);
        using var g   = System.Drawing.Graphics.FromImage(fb);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(System.Drawing.Color.Transparent);
        using var bgBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(99, 102, 241));
        g.FillEllipse(bgBrush, 1, 1, 30, 30);
        using var pen = new System.Drawing.Pen(System.Drawing.Color.White, 2.5f);
        g.DrawLine(pen, 16, 8, 16, 16);
        g.DrawLine(pen, 16, 16, 22, 20);
        var fIcon = fb.GetHicon();
        var fallback = (Icon)Icon.FromHandle(fIcon).Clone();
        NativeMethods.DestroyIcon(fIcon);
        return fallback;
    }
}
