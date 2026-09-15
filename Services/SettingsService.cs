using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinTime.Services;

/// <summary>
/// Хранит и сохраняет пользовательские настройки в %AppData%\WinTime\settings.json.
/// </summary>
public sealed class SettingsService
{
    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinTime");

    private static readonly string SettingsPath =
        Path.Combine(SettingsDir, "settings.json");

    private static readonly JsonSerializerOptions _json =
        new() { WriteIndented = true };

    private AppSettings _s = new();

    // Properties

    /// <summary>Путь к файлу SQLite. null если пользователь ещё не выбрал.</summary>
    public string? DatabasePath
    {
        get => _s.DatabasePath;
        set { _s.DatabasePath = value; Save(); }
    }

    /// <summary>true если пользователь уже выбрал путь к БД.</summary>
    public bool IsDatabaseConfigured =>
        !string.IsNullOrWhiteSpace(_s.DatabasePath);

    /// <summary>Порог AFK в секундах (default 120).</summary>
    public int AfkThresholdSeconds
    {
        get => _s.AfkThresholdSeconds;
        set { _s.AfkThresholdSeconds = value; Save(); }
    }

    public bool LaunchOnStartup
    {
        get => _s.LaunchOnStartup;
        set { _s.LaunchOnStartup = value; Save(); }
    }

    public bool ShowWidget
    {
        get => _s.ShowWidget;
        set { _s.ShowWidget = value; Save(); }
    }

    public double WidgetLeft
    {
        get => _s.WidgetLeft;
        set { _s.WidgetLeft = value; Save(); }
    }

    public double WidgetTop
    {
        get => _s.WidgetTop;
        set { _s.WidgetTop = value; Save(); }
    }

    public bool WidgetShowApp
    {
        get => _s.WidgetShowApp;
        set { _s.WidgetShowApp = value; Save(); }
    }

    public bool WidgetShowTime
    {
        get => _s.WidgetShowTime;
        set { _s.WidgetShowTime = value; Save(); }
    }

    public bool WidgetShowLevel
    {
        get => _s.WidgetShowLevel;
        set { _s.WidgetShowLevel = value; Save(); }
    }

    public bool WidgetShowMouse
    {
        get => _s.WidgetShowMouse;
        set { _s.WidgetShowMouse = value; Save(); }
    }

    // Load / Save

    public void Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                _s = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            _s = new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(_s, _json));
        }
        catch { /* не ломаем приложение из-за настроек */ }
    }

    // Internal model

    private sealed class AppSettings
    {
        public string? DatabasePath { get; set; }
        public int AfkThresholdSeconds { get; set; } = 120;
        public bool LaunchOnStartup { get; set; }
        public bool ShowWidget { get; set; } = false;
        public double WidgetLeft { get; set; } = -1;
        public double WidgetTop { get; set; } = -1;
        public bool WidgetShowApp { get; set; } = true;
        public bool WidgetShowTime { get; set; } = true;
        public bool WidgetShowLevel { get; set; } = true;
        public bool WidgetShowMouse { get; set; } = true;
    }
}

