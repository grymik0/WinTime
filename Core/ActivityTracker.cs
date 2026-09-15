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
    private readonly UptimeRepository      _uptimeRepo;
    private readonly InMemoryBuffer        _buffer;
    private readonly SettingsService       _settings;

    private CancellationTokenSource _cts = new();
    private Task? _workerTask;
    private Task? _flushTask;

    private readonly object _uptimeLock = new();
    private readonly Dictionary<int, int> _uptimeBuffer = [];
    private volatile HashSet<int> _runningAppIds = [];

    // ── Mouse tracking
    private Task? _mouseTask;
    private long _pendingClicks;
    private double _pendingDistancePixels;
    private long _totalClicksToday;
    private double _totalDistanceMetersToday;
    private readonly object _mouseLock = new();

    // ── Filtry

    private static readonly HashSet<string> IgnoredClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Shell_TrayWnd", "Progman", "WorkerW",
        "Shell_SecondaryTrayWnd", "DV2ControlHost", "Windows.UI.Core.CoreWindow"
    };

    private static readonly HashSet<string> IgnoredProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "LockApp", "LogonUI", "ShellExperienceHost",
        "SearchHost", "StartMenuExperienceHost", "TextInputHost", "ApplicationFrameHost", "SystemSettings"
    };

    // ── State

    private volatile bool _isPaused;

    public bool IsPaused
    {
        get => _isPaused;
        set => _isPaused = value;
    }

    /// <summary>Возвращает набор ID приложений, чьи окна открыты прямо сейчас.</summary>
    public HashSet<int> GetRunningAppIds() => _runningAppIds;

    /// <summary>Вызывается при каждом обновлении (раз в секунду) из UI-потока или фонового.</summary>
    public event EventHandler<TrackerStateEventArgs>? StateChanged;

    // ── Constructor

    public ActivityTracker(
        ApplicationRepository appRepo,
        ActivityRepository    activityRepo,
        UptimeRepository      uptimeRepo,
        InMemoryBuffer        buffer,
        SettingsService       settings)
    {
        _appRepo      = appRepo;
        _activityRepo = activityRepo;
        _uptimeRepo   = uptimeRepo;
        _buffer       = buffer;
        _settings     = settings;
    }

    public (long Clicks, double DistanceMeters) GetTodayMouseMetrics()
    {
        lock (_mouseLock)
        {
            return (_totalClicksToday, _totalDistanceMetersToday);
        }
    }

    public async Task InitTodayMouseMetricsAsync()
    {
        try
        {
            var (clicks, dist) = await _activityRepo.GetDailyMetricsAsync(DateTime.Today, DateTime.Today.AddDays(1));
            lock (_mouseLock)
            {
                _totalClicksToday = clicks + _pendingClicks;
                _totalDistanceMetersToday = dist + (_pendingDistancePixels * 0.0002645833);
            }
        }
        catch { }
    }

    // ── Lifecycle

    public void Start()
    {
        _cts         = new CancellationTokenSource();
        _ = InitTodayMouseMetricsAsync();
        _workerTask  = Task.Run(() => TrackLoopAsync(_cts.Token));
        _flushTask   = Task.Run(() => FlushLoopAsync(_cts.Token));
        _mouseTask   = Task.Run(() => MouseLoopAsync(_cts.Token));
    }

    public async Task StopAsync()
    {
        await _cts.CancelAsync();
        try
        {
            if (_workerTask is not null) await _workerTask;
            if (_flushTask  is not null) await _flushTask;
            if (_mouseTask  is not null) await _mouseTask;
        }
        catch (OperationCanceledException) { }

        await FlushToDbAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cts.Dispose();
    }

    // ── Worker loops

    private async Task MouseLoopAsync(CancellationToken ct)
    {
        // 96 DPI standard: 1 inch = 2.54 cm = 0.0254 m. 1 px = 0.0254 / 96 = ~0.0002645833 meters
        const double metersPerPixel = 0.0002645833;
        NativeMethods.POINT lastPt = default;
        bool hasLastPt = false;

        bool prevLeft = false;
        bool prevRight = false;
        bool prevMiddle = false;

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(25));
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                if (_isPaused) continue;

                if (NativeMethods.GetCursorPos(out var pt))
                {
                    if (hasLastPt)
                    {
                        int dx = pt.X - lastPt.X;
                        int dy = pt.Y - lastPt.Y;
                        if (dx != 0 || dy != 0)
                        {
                            double distPx = Math.Sqrt(dx * dx + dy * dy);
                            lock (_mouseLock)
                            {
                                _pendingDistancePixels += distPx;
                                _totalDistanceMetersToday += distPx * metersPerPixel;
                            }
                        }
                    }
                    lastPt = pt;
                    hasLastPt = true;
                }

                // Check button transitions (key down edge)
                bool curLeft   = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LBUTTON) & 0x8000) != 0;
                bool curRight  = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_RBUTTON) & 0x8000) != 0;
                bool curMiddle = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_MBUTTON) & 0x8000) != 0;

                int clicks = 0;
                if (curLeft && !prevLeft) clicks++;
                if (curRight && !prevRight) clicks++;
                if (curMiddle && !prevMiddle) clicks++;

                prevLeft   = curLeft;
                prevRight  = curRight;
                prevMiddle = curMiddle;

                if (clicks > 0)
                {
                    lock (_mouseLock)
                    {
                        _pendingClicks += clicks;
                        _totalClicksToday += clicks;
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task TrackLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                if (_isPaused) continue;
                try { await ProcessTickAsync(); }
                catch { }
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

    // ── Tick processing

    private async Task ProcessTickAsync()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return;

        var cls = new StringBuilder(256);
        NativeMethods.GetClassName(hwnd, cls, 256);
        if (IgnoredClasses.Contains(cls.ToString())) return;

        NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
        if (pid == 0) return;

        string processName, processPath;
        try
        {
            (processName, processPath) = GetProcessInfo(pid);
        }
        catch { return; }

        if (string.IsNullOrEmpty(processName)) return;

        var baseName = Path.GetFileNameWithoutExtension(processName);
        if (IgnoredProcesses.Contains(baseName)) return;

        var titleBuf = new StringBuilder(512);
        NativeMethods.GetWindowText(hwnd, titleBuf, 512);
        var windowTitle = titleBuf.ToString();

        if (string.IsNullOrWhiteSpace(windowTitle))
        {
            var rootHwnd = NativeMethods.GetAncestor(hwnd, NativeMethods.GA_ROOT);
            if (rootHwnd != IntPtr.Zero && rootHwnd != hwnd)
            {
                var rootBuf = new StringBuilder(512);
                NativeMethods.GetWindowText(rootHwnd, rootBuf, 512);
                windowTitle = rootBuf.ToString();
            }

            if (string.IsNullOrWhiteSpace(windowTitle))
            {
                var ownerHwnd = NativeMethods.GetAncestor(hwnd, NativeMethods.GA_ROOTOWNER);
                if (ownerHwnd != IntPtr.Zero && ownerHwnd != hwnd && ownerHwnd != rootHwnd)
                {
                    var ownerBuf = new StringBuilder(512);
                    NativeMethods.GetWindowText(ownerHwnd, ownerBuf, 512);
                    windowTitle = ownerBuf.ToString();
                }
            }
        }

        var lii = new NativeMethods.LASTINPUTINFO
        {
            cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.LASTINPUTINFO>()
        };
        NativeMethods.GetLastInputInfo(ref lii);
        uint idleMs = (uint)Environment.TickCount - lii.dwTime;
        bool isIdle = idleMs > _settings.AfkThresholdSeconds * 1000u;

        var app = await _appRepo.GetOrCreateAsync(processName, processPath);
        if (!app.IsBlacklisted)
        {
            _buffer.Update(app.Id, windowTitle, isIdle, DateTime.Now);

            StateChanged?.Invoke(this, new TrackerStateEventArgs(
                app.Id, app.FriendlyName, windowTitle, isIdle));
        }

        await ScanRunningWindowsAsync();
    }

    private async Task ScanRunningWindowsAsync()
    {
        var runningPids = new HashSet<uint>();

        NativeMethods.EnumWindows((hWnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hWnd)) return true;

            long exStyle = NativeMethods.GetWindowLongPtr(hWnd, NativeMethods.GWL_EXSTYLE);
            if ((exStyle & NativeMethods.WS_EX_TOOLWINDOW) != 0) return true;

            var cls = new StringBuilder(256);
            NativeMethods.GetClassName(hWnd, cls, 256);
            if (IgnoredClasses.Contains(cls.ToString())) return true;

            var title = new StringBuilder(256);
            NativeMethods.GetWindowText(hWnd, title, 256);
            if (title.Length == 0) return true;

            NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);
            if (pid != 0)
                runningPids.Add(pid);

            return true;
        }, IntPtr.Zero);

        var currentAppIds = new HashSet<int>();
        var currentPid = (uint)Environment.ProcessId;

        foreach (var pid in runningPids)
        {
            if (pid == currentPid) continue;

            string procName, procPath;
            try { (procName, procPath) = GetProcessInfo(pid); }
            catch { continue; }

            if (string.IsNullOrEmpty(procName)) continue;
            var baseName = Path.GetFileNameWithoutExtension(procName);
            if (IgnoredProcesses.Contains(baseName)) continue;

            try
            {
                var app = await _appRepo.GetOrCreateAsync(procName, procPath);
                if (app.IsBlacklisted) continue;

                currentAppIds.Add(app.Id);

                lock (_uptimeLock)
                {
                    _uptimeBuffer[app.Id] = _uptimeBuffer.GetValueOrDefault(app.Id, 0) + 1;
                }
            }
            catch { }
        }

        _runningAppIds = currentAppIds;
    }

    public async Task FlushToDbAsync()
    {
        try
        {
            var sessions = _buffer.Flush();
            if (sessions.Count > 0)
                await _activityRepo.InsertBatchAsync(sessions);

            List<(int AppId, int Seconds)> uptimeList;
            lock (_uptimeLock)
            {
                uptimeList = _uptimeBuffer.Select(kv => (kv.Key, kv.Value)).ToList();
                _uptimeBuffer.Clear();
            }

            if (uptimeList.Count > 0)
            {
                var today = DateTime.Now.ToString("yyyy-MM-dd");
                await _uptimeRepo.AddUptimeBatchAsync(today, uptimeList);
            }

            long clicksToSave;
            double distMetersToSave;
            lock (_mouseLock)
            {
                clicksToSave = _pendingClicks;
                distMetersToSave = _pendingDistancePixels * 0.0002645833;
                _pendingClicks = 0;
                _pendingDistancePixels = 0;
            }

            if (clicksToSave > 0 || distMetersToSave > 0.01)
            {
                var today = DateTime.Now.ToString("yyyy-MM-dd");
                await _activityRepo.SaveDailyMetricsAsync(today, clicksToSave, distMetersToSave);
            }
        }
        catch { }
    }

    // ── Helpers

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
public sealed class TrackerStateEventArgs(int appId, string appName, string windowTitle, bool isIdle) : EventArgs
{
    public int    AppId       { get; } = appId;
    public string AppName     { get; } = appName;
    public string WindowTitle { get; } = windowTitle;
    public bool   IsIdle      { get; } = isIdle;
}

