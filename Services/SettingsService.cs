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

    // ── Properties ──────────────────────────────────────────────────────────

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

    // ── Load / Save ─────────────────────────────────────────────────────────

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

    // ── Internal model ───────────────────────────────────────────────────────

    private sealed class AppSettings
    {
        public string? DatabasePath { get; set; }
        public int AfkThresholdSeconds { get; set; } = 120;
        public bool LaunchOnStartup { get; set; }
    }
}
