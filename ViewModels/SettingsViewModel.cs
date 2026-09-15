using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using WinTime.Core;
using WinTime.Data;
using WinTime.Services;

namespace WinTime.ViewModels;

/// <summary>
/// ViewModel экрана «Настройки»:
/// — AFK-порог (слайдер)
/// — Автозапуск Windows (реестр)
/// — Путь к БД (выбор файла)
/// — Экспорт данных
/// — Очистка истории
/// </summary>
public sealed class SettingsViewModel : BaseViewModel
{
    private readonly SettingsService   _settings;
    private readonly ActivityRepository _activityRepo;
    private readonly ExportService     _exportService;

    private int    _afkThresholdMinutes;
    private bool   _launchOnStartup;
    private string _databasePath = string.Empty;
    private string _dbChangeNote = string.Empty;

    // Properties

    public int AfkThresholdMinutes
    {
        get => _afkThresholdMinutes;
        set
        {
            if (SetProperty(ref _afkThresholdMinutes, Math.Clamp(value, 1, 10)))
                _settings.AfkThresholdSeconds = value * 60;
        }
    }

    public bool LaunchOnStartup
    {
        get => _launchOnStartup;
        set
        {
            if (SetProperty(ref _launchOnStartup, value))
            {
                _settings.LaunchOnStartup = value;
                var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                if (value) SystemStartupManager.Enable(exePath);
                else       SystemStartupManager.Disable();
            }
        }
    }

    public string DatabasePath
    {
        get => _databasePath;
        private set => SetProperty(ref _databasePath, value);
    }

    /// <summary>Подсказка пользователю, что смена пути вступит в силу после перезапуска.</summary>
    public string DbChangeNote
    {
        get => _dbChangeNote;
        private set => SetProperty(ref _dbChangeNote, value);
    }

    // Commands

    public ICommand ChooseDbPathCommand  { get; }
    public ICommand ExportCsvCommand     { get; }
    public ICommand ExportJsonCommand    { get; }
    public ICommand ClearHistoryCommand  { get; }

    // Constructor

    public SettingsViewModel(
        SettingsService    settings,
        ActivityRepository activityRepo,
        ExportService      exportService)
    {
        _settings      = settings;
        _activityRepo  = activityRepo;
        _exportService = exportService;

        _afkThresholdMinutes = Math.Max(1, settings.AfkThresholdSeconds / 60);
        _launchOnStartup     = SystemStartupManager.IsEnabled();
        _databasePath        = settings.DatabasePath ?? string.Empty;

        ChooseDbPathCommand = new RelayCommand(ChooseDbPath);
        ExportCsvCommand    = new RelayCommand(async () => await ExportCsvAsync());
        ExportJsonCommand   = new RelayCommand(async () => await ExportJsonAsync());
        ClearHistoryCommand = new RelayCommand(async () => await ClearHistoryAsync());
    }

    // Handlers

    private void ChooseDbPath()
    {
        var dlg = new SaveFileDialog
        {
            Title            = "Выберите или создайте файл базы данных",
            Filter           = "SQLite Database (*.db)|*.db|All files (*.*)|*.*",
            FileName         = "wintime.db",
            InitialDirectory = System.IO.Path.GetDirectoryName(_databasePath)
                               ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            OverwritePrompt  = false
        };

        if (dlg.ShowDialog() == true)
        {
            _settings.DatabasePath = dlg.FileName;
            DatabasePath   = dlg.FileName;
            DbChangeNote   = "⚠ Смена пути вступит в силу после перезапуска WinTime.";
        }
    }

    private async Task ExportCsvAsync()
    {
        var dlg = new SaveFileDialog
        {
            Title    = "Экспортировать в CSV",
            Filter   = "CSV files (*.csv)|*.csv",
            FileName = $"wintime_{DateTime.Now:yyyyMMdd}.csv"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            await _exportService.ExportToCsvAsync(dlg.FileName);
            MessageBox.Show($"Экспорт завершён:\n{dlg.FileName}", "WinTime",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка экспорта: {ex.Message}", "WinTime",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ExportJsonAsync()
    {
        var dlg = new SaveFileDialog
        {
            Title    = "Экспортировать в JSON",
            Filter   = "JSON files (*.json)|*.json",
            FileName = $"wintime_{DateTime.Now:yyyyMMdd}.json"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            await _exportService.ExportToJsonAsync(dlg.FileName);
            MessageBox.Show($"Экспорт завершён:\n{dlg.FileName}", "WinTime",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка экспорта: {ex.Message}", "WinTime",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ClearHistoryAsync()
    {
        var res = MessageBox.Show(
            "Вы уверены, что хотите удалить всю историю активности?\nОтменить это действие невозможно.",
            "Очистка истории", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (res == MessageBoxResult.Yes)
        {
            await _activityRepo.ClearAllAsync();
            MessageBox.Show("История очищена.", "WinTime",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

