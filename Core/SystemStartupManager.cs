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
}

