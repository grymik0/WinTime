using System.Diagnostics;
using System.Windows;
using WinTime.Core;
using WinTime.Data;
using WinTime.Models;

namespace WinTime.Services;

/// <summary>
/// Monitors active application screen time against configured daily limits.
/// Enforces either periodic notifications or process termination with countdown.
/// </summary>
public sealed class LimitEnforcerService
{
    private readonly GoalRepository     _goalRepo;
    private readonly ActivityRepository _activityRepo;
    private readonly ActivityTracker    _tracker;
    private readonly LocalizationService _localization;

    // Cache of limits: AppId -> AppLimitItem
    private readonly Dictionary<int, AppLimitItem> _limitsCache = new();
    // Cache of today's accumulated active seconds: AppId -> active seconds
    private readonly Dictionary<int, int> _todayUsageCache = new();

    // Track last notification time per AppId to avoid spamming
    private readonly Dictionary<int, DateTime> _lastNotificationTime = new();

    // Kill countdown state: AppId -> Warning start time
    private readonly Dictionary<int, DateTime> _pendingKillWarnings = new();

    private DateTime _cacheDate = DateTime.Today;

    public LimitEnforcerService(
        GoalRepository goalRepo,
        ActivityRepository activityRepo,
        ActivityTracker tracker,
        LocalizationService localization)
    {
        _goalRepo     = goalRepo;
        _activityRepo = activityRepo;
        _tracker      = tracker;
        _localization = localization;

        _tracker.StateChanged += OnTrackerStateChanged;
    }

    public async Task ReloadLimitsAsync()
    {
        try
        {
            var limits = await _goalRepo.GetAllLimitsAsync();
            lock (_limitsCache)
            {
                _limitsCache.Clear();
                foreach (var lim in limits)
                {
                    _limitsCache[lim.AppId] = lim;
                }
            }

            // Sync today's usage from DB
            var today = DateTime.Today;
            _cacheDate = today;
            var totals = await _activityRepo.GetTopAppsAsync(today, today.AddDays(1));
            lock (_todayUsageCache)
            {
                _todayUsageCache.Clear();
                foreach (var t in totals)
                {
                    _todayUsageCache[t.AppId] = (int)t.TotalSeconds;
                }
            }
        }
        catch { }
    }

    public (int LimitSec, int UsedSec, bool IsExceeded)? GetLimitStatus(int appId)
    {
        lock (_limitsCache)
        {
            if (!_limitsCache.TryGetValue(appId, out var lim) || !lim.IsEnabled)
                return null;

            int used = 0;
            lock (_todayUsageCache)
            {
                _todayUsageCache.TryGetValue(appId, out used);
            }

            return (lim.MaxDailySeconds, used, used >= lim.MaxDailySeconds);
        }
    }

    private void OnTrackerStateChanged(object? sender, TrackerStateEventArgs e)
    {
        if (e.IsIdle || e.AppId <= 0) return;

        // Check if date changed
        if (DateTime.Today != _cacheDate)
        {
            _ = ReloadLimitsAsync();
            return;
        }

        AppLimitItem? limit = null;
        lock (_limitsCache)
        {
            if (!_limitsCache.TryGetValue(e.AppId, out limit) || !limit.IsEnabled)
                return;
        }

        int currentUsed;
        lock (_todayUsageCache)
        {
            if (!_todayUsageCache.TryGetValue(e.AppId, out currentUsed))
                currentUsed = 0;

            currentUsed++;
            _todayUsageCache[e.AppId] = currentUsed;
        }

        if (currentUsed >= limit.MaxDailySeconds)
        {
            HandleLimitExceeded(e.AppId, e.AppName, limit, currentUsed);
        }
    }

    private void HandleLimitExceeded(int appId, string appName, AppLimitItem limit, int usedSeconds)
    {
        var now = DateTime.UtcNow;

        if (limit.ActionType == LimitActionType.Notify)
        {
            // Notify at most once every 3 minutes
            if (_lastNotificationTime.TryGetValue(appId, out var lastNotif) &&
                (now - lastNotif).TotalMinutes < 3)
            {
                return;
            }

            _lastNotificationTime[appId] = now;

            string friendlyName = !string.IsNullOrWhiteSpace(limit.DisplayName) ? limit.DisplayName : appName;
            string usedStr = LocalizationService.FormatDuration(usedSeconds);
            string limitStr = LocalizationService.FormatDuration(limit.MaxDailySeconds);

            bool isRu = _localization.CurrentLanguage == AppLanguage.Ru;
            string title = isRu ? "⚠️ Лимит времени исчерпан!" : "⚠️ Time limit reached!";
            string msg = isRu
                ? $"Приложение «{friendlyName}» достигло установленного дневного лимита ({limitStr}).\nВы используете его уже {usedStr}."
                : $"Application \"{friendlyName}\" has reached its daily limit ({limitStr}).\nYou have been using it for {usedStr}.";

            Task.Run(() =>
            {
                NativeMethods.MessageBox(
                    IntPtr.Zero,
                    msg,
                    title,
                    NativeMethods.MB_OK |
                    NativeMethods.MB_ICONWARNING |
                    NativeMethods.MB_TOPMOST |
                    NativeMethods.MB_SETFOREGROUND |
                    NativeMethods.MB_SYSTEMMODAL);
            });
        }
        else if (limit.ActionType == LimitActionType.CloseProcess)
        {
            if (!_pendingKillWarnings.TryGetValue(appId, out var warningStart))
            {
                // Start 60-second warning countdown
                _pendingKillWarnings[appId] = now;
                string friendlyName = !string.IsNullOrWhiteSpace(limit.DisplayName) ? limit.DisplayName : appName;
                bool isRu = _localization.CurrentLanguage == AppLanguage.Ru;

                string title = isRu ? "🛑 ПРИНУДИТЕЛЬНОЕ ЗАКРЫТИЕ" : "🛑 PROCESS SHUTDOWN";
                string msg = isRu
                    ? $"Лимит времени для «{friendlyName}» превышен!\nУ вас есть 60 секунд, чтобы сохранить результаты работы или игру, после чего процесс будет принудительно закрыт."
                    : $"Time limit for \"{friendlyName}\" exceeded!\nYou have 60 seconds to save your progress before the process is closed.";

                Task.Run(() =>
                {
                    NativeMethods.MessageBox(
                        IntPtr.Zero,
                        msg,
                        title,
                        NativeMethods.MB_OK |
                        NativeMethods.MB_ICONSTOP |
                        NativeMethods.MB_TOPMOST |
                        NativeMethods.MB_SETFOREGROUND |
                        NativeMethods.MB_SYSTEMMODAL);
                });
            }
            else
            {
                // Check if 60 seconds have elapsed
                if ((now - warningStart).TotalSeconds >= 60)
                {
                    _pendingKillWarnings.Remove(appId);
                    KillProcessByName(limit.ProcessName);
                }
            }
        }
    }

    private static void KillProcessByName(string processName)
    {
        try
        {
            var nameOnly = System.IO.Path.GetFileNameWithoutExtension(processName);
            var procs = Process.GetProcessesByName(nameOnly);
            foreach (var p in procs)
            {
                try
                {
                    // Attempt clean close first
                    if (!p.CloseMainWindow())
                    {
                        p.Kill();
                    }
                }
                catch { }
            }
        }
        catch { }
    }
}
