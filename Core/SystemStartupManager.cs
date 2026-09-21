using Microsoft.Win32;

namespace WinTime.Core;

/// <summary>
/// Manages Windows startup registration via CurrentUser Run registry key.
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
    /// Synchronizes startup registry entry with the current executable path.
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

