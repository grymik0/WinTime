using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinTime.Services;

/// <summary>
/// Persists and manages user configuration in %AppData%\WinTime\settings.json.
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

    public string? DatabasePath
    {
        get => _s.DatabasePath;
        set { _s.DatabasePath = value; Save(); }
    }

    public bool IsDatabaseConfigured =>
        !string.IsNullOrWhiteSpace(_s.DatabasePath);

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

    public double WidgetOpacity
    {
        get => _s.WidgetOpacity;
        set { _s.WidgetOpacity = Math.Clamp(value, 0.3, 1.0); Save(); }
    }

    public bool WidgetClickThrough
    {
        get => _s.WidgetClickThrough;
        set { _s.WidgetClickThrough = value; Save(); }
    }

    public bool WidgetTopmost
    {
        get => _s.WidgetTopmost;
        set { _s.WidgetTopmost = value; Save(); }
    }

    public bool WidgetShowSession
    {
        get => _s.WidgetShowSession;
        set { _s.WidgetShowSession = value; Save(); }
    }

    public bool WidgetCompactMode
    {
        get => _s.WidgetCompactMode;
        set { _s.WidgetCompactMode = value; Save(); }
    }

    public string ThemeMode
    {
        get => _s.ThemeMode;
        set { _s.ThemeMode = value; Save(); }
    }

    public string AccentColor
    {
        get => _s.AccentColor;
        set { _s.AccentColor = value; Save(); }
    }

    public string Language
    {
        get => _s.Language;
        set { _s.Language = value; Save(); }
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
        catch { }
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
        public double WidgetOpacity { get; set; } = 0.95;
        public bool WidgetClickThrough { get; set; } = false;
        public bool WidgetTopmost { get; set; } = true;
        public bool WidgetShowSession { get; set; } = true;
        public bool WidgetCompactMode { get; set; } = false;
        public string ThemeMode { get; set; } = "Dark";
        public string AccentColor { get; set; } = "Indigo";
        public string Language { get; set; } = "Ru";
    }
}

