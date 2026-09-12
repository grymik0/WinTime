using System.Drawing;
using System.Threading;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using WinTime.Core;
using WinTime.Services;
using WinTime.Views;

namespace WinTime;

/// <summary>
/// Точка входа приложения.
/// 1. Проверяем единственный экземпляр (Mutex)
/// 2. Загружаем настройки
/// 3. Первый запуск → диалог выбора пути к БД
/// 4. Инициализируем AppServices (DB, трекер, ViewModel'ы)
/// 5. Запускаем ActivityTracker
/// 6. Создаём иконку в системном трее
/// </summary>
public partial class App : Application
{
    private TaskbarIcon?  _trayIcon;
    private MainWindow?   _mainWindow;
    private bool          _isExiting;
    private Mutex?        _singleInstanceMutex;

    /// <summary>Читается в MainWindow.Window_Closing для различия Hide vs реального выхода.</summary>
    public bool IsExiting => _isExiting;

    // ── Startup ───────────────────────────────────────────────────────────────

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Одновременно может работать только один экземпляр приложения
        _singleInstanceMutex = new Mutex(true, "WinTime_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "WinTime уже запущен.\nНайдите иконку в системном трее (правый нижний угол экрана).",
                "WinTime", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // Перехватываем необработанные исключения — показываем ошибку, не роняем процесс
        DispatcherUnhandledException += (_, ex) =>
        {
            ex.Handled = true;
            MessageBox.Show(
                $"Произошла непредвиденная ошибка:\n\n{ex.Exception.Message}",
                "WinTime — Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        // 1. Настройки
        var settings = new SettingsService();
        settings.Load();

        // 2. Первый запуск
        if (!settings.IsDatabaseConfigured)
        {
            var dlg = new FirstRunDialog();
            if (dlg.ShowDialog() != true)
            {
                Shutdown();
                return;
            }
            settings.DatabasePath = dlg.SelectedDbPath;
        }

        // 3. Инициализируем все сервисы и ViewModel'ы
        try
        {
            AppServices.Initialize(settings);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Не удалось открыть базу данных:\n{settings.DatabasePath}\n\n{ex.Message}",
                "WinTime — Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        // 4. Запуск трекера
        AppServices.Tracker.Start();

        // 5. Иконка в трее (без файла иконки — генерируем программно)
        SetupTrayIcon();

        // 6. Создаём и показываем главное окно
        _mainWindow = new MainWindow();
        _mainWindow.Show();
    }

    // ── Exit ──────────────────────────────────────────────────────────────────

    protected override async void OnExit(ExitEventArgs e)
    {
        _isExiting = true;
        _trayIcon?.Dispose();

        // Финальный сброс буфера и закрытие БД
        await AppServices.ShutdownAsync();

        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();

        base.OnExit(e);
    }

    // ── Tray ──────────────────────────────────────────────────────────────────

    private void SetupTrayIcon()
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "WinTime — Учёт экранного времени",
            Icon        = CreateTrayIcon()
        };

        // Двойной клик — открываем дашборд
        _trayIcon.TrayMouseDoubleClick += (_, _) => ShowMainWindow();

        // Контекстное меню
        var menu = new System.Windows.Controls.ContextMenu();

        var itemOpen = new System.Windows.Controls.MenuItem { Header = "📊  Открыть статистику" };
        itemOpen.Click += (_, _) => ShowMainWindow();

        var itemPause = new System.Windows.Controls.MenuItem { Header = "⏸  Приостановить учёт" };
        itemPause.Click += (_, _) =>
        {
            AppServices.Tracker.IsPaused = !AppServices.Tracker.IsPaused;
            itemPause.Header = AppServices.Tracker.IsPaused
                ? "▶  Возобновить учёт"
                : "⏸  Приостановить учёт";
            _trayIcon.ToolTipText = AppServices.Tracker.IsPaused
                ? "WinTime — Пауза"
                : "WinTime — Учёт экранного времени";
        };

        var itemSettings = new System.Windows.Controls.MenuItem { Header = "⚙  Настройки" };
        itemSettings.Click += (_, _) =>
        {
            ShowMainWindow();
            AppServices.MainWindowVm.NavigateSettingsCommand.Execute(null);
        };

        var itemExit = new System.Windows.Controls.MenuItem { Header = "✕  Выход" };
        itemExit.Click += (_, _) => ExitApp();

        menu.Items.Add(itemOpen);
        menu.Items.Add(new System.Windows.Controls.Separator());
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

    // ── Icon generation ───────────────────────────────────────────────────────

    /// <summary>
    /// Программная генерация иконки 16×16 в виде синего круга.
    /// Заменить на реальный .ico файл — добавить в csproj:
    /// &lt;ApplicationIcon&gt;Assets\icon.ico&lt;/ApplicationIcon&gt;
    /// </summary>
    private static Icon CreateTrayIcon()
    {
        using var bmp = new System.Drawing.Bitmap(32, 32);
        using var g   = System.Drawing.Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(System.Drawing.Color.Transparent);

        // Фон — тёмно-синий круг
        using var bgBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(99, 102, 241));
        g.FillEllipse(bgBrush, 1, 1, 30, 30);

        // Символ ⏱ заменяем на простую белую точку/линию
        using var pen = new System.Drawing.Pen(System.Drawing.Color.White, 2.5f);
        g.DrawLine(pen, 16, 8, 16, 16);
        g.DrawLine(pen, 16, 16, 22, 20);

        var hIcon = bmp.GetHicon();
        // FromHandle копирует хэндл — исходный нужно освобождать
        var icon = (Icon)Icon.FromHandle(hIcon).Clone();
        NativeMethods.DestroyIcon(hIcon);
        return icon;
    }
}
