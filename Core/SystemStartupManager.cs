using Microsoft.Win32;

namespace WinTime.Core;

/// <summary>
/// Управляет автозапуском приложения через реестр Windows.
/// Ключ: HKCU\Software\Microsoft\Windows\CurrentVersion\Run
/// </summary>
public static class SystemStartupManager
{
    private const string RegKey  = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "WinTime";

    public static void Enable(string exePath)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegKey, writable: true);
            key?.SetValue(AppName, $"\"{exePath}\"");
        }
        catch { }
    }

    public static void Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegKey, writable: true);
            key?.DeleteValue(AppName, throwOnMissingValue: false);
        }
        catch { }
    }

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegKey, writable: false);
            return key?.GetValue(AppName) is not null;
        }
        catch { return false; }
    }

    /// <summary>
    /// Автоматически обновляет путь в автозапуске на текущий запущенный EXE-файл,
    /// если автозапуск был включен или если в реестре остался путь от старой версии.
    /// </summary>
    public static void SyncCurrentExePath(bool forceEnableIfConfigured = false)
    {
        try
        {
            var currentExe = Environment.ProcessPath;
            if (string.IsNullOrEmpty(currentExe)) return;

            using var key = Registry.CurrentUser.OpenSubKey(RegKey, writable: true);
            if (key is null) return;

            var existing = key.GetValue(AppName) as string;
            if (existing is not null || forceEnableIfConfigured)
            {
                var expected = $"\"{currentExe}\"";
                if (!string.Equals(existing, expected, StringComparison.OrdinalIgnoreCase))
                {
                    key.SetValue(AppName, expected);
                }
            }
        }
        catch { }
    }
}

