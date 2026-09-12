using System.Diagnostics;
using System.IO;
using System.Text;
using WinTime.Data;
using WinTime.Services;

namespace WinTime.Core;

/// <summary>
/// Фоновый поток опроса активного окна (1 раз в секунду).
/// Детектирует AFK через GetLastInputInfo, фильтрует системные окна,
/// обновляет InMemoryBuffer и периодически сбрасывает данные в SQLite.
/// </summary>
public sealed class ActivityTracker : IAsyncDisposable
{
    private readonly ApplicationRepository _appRepo;
    private readonly ActivityRepository    _activityRepo;
    private readonly InMemoryBuffer        _buffer;
    private readonly SettingsService       _settings;

    private CancellationTokenSource _cts = new();
    private Task? _workerTask;
    private Task? _flushTask;

    // ── Filtry ───────────────────────────────────────────────────────────────

    private static readonly HashSet<string> IgnoredClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Shell_TrayWnd", "Progman", "WorkerW",
        "Shell_SecondaryTrayWnd", "DV2ControlHost"
    };

    private static readonly HashSet<string> IgnoredProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "LockApp", "LogonUI", "ShellExperienceHost",
        "SearchHost", "StartMenuExperienceHost"
    };

    // ── State ─────────────────────────────────────────────────────────────────

    private volatile bool _isPaused;

    public bool IsPaused
    {
        get => _isPaused;
        set => _isPaused = value;
    }

    /// <summary>Вызывается при каждом обновлении (раз в секунду) из UI-потока или фонового.</summary>
    public event EventHandler<TrackerStateEventArgs>? StateChanged;

    // ── Constructor ───────────────────────────────────────────────────────────

    public ActivityTracker(
        ApplicationRepository appRepo,
        ActivityRepository    activityRepo,
        InMemoryBuffer        buffer,
        SettingsService       settings)
    {
        _appRepo      = appRepo;
        _activityRepo = activityRepo;
        _buffer       = buffer;
        _settings     = settings;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public void Start()
    {
        _cts         = new CancellationTokenSource();
        _workerTask  = Task.Run(() => TrackLoopAsync(_cts.Token));
        _flushTask   = Task.Run(() => FlushLoopAsync(_cts.Token));
    }

    public async Task StopAsync()
    {
        await _cts.CancelAsync();
        try
        {
            if (_workerTask is not null) await _workerTask;
            if (_flushTask  is not null) await _flushTask;
        }
        catch (OperationCanceledException) { }

        // Финальный принудительный сброс
        await FlushToDbAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cts.Dispose();
    }

    // ── Worker loops ─────────────────────────────────────────────────────────

    private async Task TrackLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                if (_isPaused) continue;
                try { await ProcessTickAsync(); }
                catch { /* тихо — не роняем приложение из-за одного тика */ }
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task FlushLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                await FlushToDbAsync();
            }
        }
        catch (OperationCanceledException) { }
    }

    // ── Tick processing ───────────────────────────────────────────────────────

    private async Task ProcessTickAsync()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return;

        // Фильтр по классу окна
        var cls = new StringBuilder(256);
        NativeMethods.GetClassName(hwnd, cls, 256);
        if (IgnoredClasses.Contains(cls.ToString())) return;

        // PID
        NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
        if (pid == 0) return;

        // Имя процесса и путь к EXE
        string processName, processPath;
        try
        {
            (processName, processPath) = GetProcessInfo(pid);
        }
        catch { return; } // Access Denied (UAC / System)

        if (string.IsNullOrEmpty(processName)) return;

        var baseName = Path.GetFileNameWithoutExtension(processName);
        if (IgnoredProcesses.Contains(baseName)) return;

        // Заголовок окна
        var titleBuf = new StringBuilder(512);
        NativeMethods.GetWindowText(hwnd, titleBuf, 512);
        var windowTitle = titleBuf.ToString();

        // AFK-детекция
        var lii = new NativeMethods.LASTINPUTINFO
        {
            cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.LASTINPUTINFO>()
        };
        NativeMethods.GetLastInputInfo(ref lii);
        uint idleMs = (uint)Environment.TickCount - lii.dwTime;
        bool isIdle = idleMs > _settings.AfkThresholdSeconds * 1000u;

        // Запись в БД (GetOrCreate из кэша — очень быстро)
        var app = await _appRepo.GetOrCreateAsync(processName, processPath);
        if (app.IsBlacklisted) return;

        _buffer.Update(app.Id, windowTitle, isIdle, DateTime.Now);

        StateChanged?.Invoke(this, new TrackerStateEventArgs(
            app.FriendlyName, windowTitle, isIdle));
    }

    private async Task FlushToDbAsync()
    {
        try
        {
            var sessions = _buffer.Flush();
            if (sessions.Count > 0)
                await _activityRepo.InsertBatchAsync(sessions);
        }
        catch { }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static (string name, string path) GetProcessInfo(uint pid)
    {
        var hProcess = NativeMethods.OpenProcess(
            NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);

        if (hProcess != IntPtr.Zero)
        {
            try
            {
                var sb   = new StringBuilder(1024);
                int size = 1024;
                if (NativeMethods.QueryFullProcessImageName(hProcess, 0, sb, ref size))
                {
                    var fullPath = sb.ToString();
                    return (Path.GetFileName(fullPath), fullPath);
                }
            }
            finally
            {
                NativeMethods.CloseHandle(hProcess);
            }
        }

        // Fallback: System.Diagnostics.Process (менее надёжно, но работает)
        try
        {
            using var p = Process.GetProcessById((int)pid);
            return (p.ProcessName + ".exe", string.Empty);
        }
        catch
        {
            return (string.Empty, string.Empty);
        }
    }
}

/// <summary>Аргументы события TrackerStateChanged.</summary>
public sealed class TrackerStateEventArgs(string appName, string windowTitle, bool isIdle) : EventArgs
{
    public string AppName     { get; } = appName;
    public string WindowTitle { get; } = windowTitle;
    public bool   IsIdle      { get; } = isIdle;
}
