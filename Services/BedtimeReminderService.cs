using System.Diagnostics;
using WinTime.Core;
using WinTime.Data;
using WinTime.Models;

namespace WinTime.Services;

/// <summary>
/// Smart bedtime recommendation engine.
/// Evaluates historical bedtime patterns over recent weeks and gently advises
/// the user 1 hour prior to their typical sleep time to conclude PC activities.
/// </summary>
public sealed class BedtimeReminderService : IDisposable
{
    private readonly ActivityRepository  _activityRepo;
    private readonly SettingsService    _settings;
    private readonly ActivityTracker    _tracker;
    private readonly LocalizationService _localization;

    private readonly System.Threading.Timer _timer;
    private bool _isChecking;

    private DateTime? _lastNotifiedCycleDate;
    private TimeSpan? _cachedBedtime;
    private DateTime  _lastBedtimeCalcUtc = DateTime.MinValue;

    public BedtimeReminderService(
        ActivityRepository activityRepo,
        SettingsService settings,
        ActivityTracker tracker,
        LocalizationService localization)
    {
        _activityRepo = activityRepo;
        _settings     = settings;
        _tracker      = tracker;
        _localization = localization;

        // Check once per minute, start after 30 seconds
        _timer = new System.Threading.Timer(OnTimerTick, null, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1));
    }

    public void Start()
    {
        // Initial warmup calculation in background
        _ = WarmupAsync();
    }

    private async Task WarmupAsync()
    {
        try
        {
            _cachedBedtime = await GetAverageBedtimeAsync();
            _lastBedtimeCalcUtc = DateTime.UtcNow;
        }
        catch { }
    }

    public async Task<TimeSpan?> GetAverageBedtimeAsync(int days = 30)
    {
        try
        {
            var rhythms = await _activityRepo.GetDailyRhythmsAsync(days);
            if (rhythms.Count == 0)
                return null;

            return CalculateCircularAverageTime(rhythms.Select(r => r.LastActive));
        }
        catch
        {
            return null;
        }
    }

    public static TimeSpan CalculateCircularAverageTime(IEnumerable<TimeSpan> times)
    {
        double sumSin = 0;
        double sumCos = 0;
        int count = 0;

        foreach (var t in times)
        {
            double fraction = t.TotalSeconds / 86400.0;
            double angle = fraction * 2.0 * Math.PI;
            sumSin += Math.Sin(angle);
            sumCos += Math.Cos(angle);
            count++;
        }

        if (count == 0) return TimeSpan.Zero;

        double meanAngle = Math.Atan2(sumSin, sumCos);
        if (meanAngle < 0) meanAngle += 2.0 * Math.PI;

        double meanFraction = meanAngle / (2.0 * Math.PI);
        double totalSeconds = meanFraction * 86400.0;

        return TimeSpan.FromSeconds(totalSeconds);
    }

    private async void OnTimerTick(object? state)
    {
        if (!_settings.BedtimeReminderEnabled) return;
        if (_isChecking) return;

        _isChecking = true;
        try
        {
            var now = DateTime.Now;
            // Sleep cycle aligns hours 00:00-04:59 with the previous evening
            DateTime cycleDate = now.Hour < 5 ? DateTime.Today.AddDays(-1) : DateTime.Today;

            if (_lastNotifiedCycleDate == cycleDate)
                return;

            // Recalculate average bedtime every 4 hours
            if (_cachedBedtime == null || (DateTime.UtcNow - _lastBedtimeCalcUtc).TotalHours >= 4)
            {
                _cachedBedtime = await GetAverageBedtimeAsync();
                _lastBedtimeCalcUtc = DateTime.UtcNow;
            }

            if (!_cachedBedtime.HasValue)
                return;

            var avgBedtime = _cachedBedtime.Value;
            double bedOffset = (avgBedtime.TotalHours - 5 + 24) % 24;
            double nowOffset = (now.TimeOfDay.TotalHours - 5 + 24) % 24;

            // Trigger window:
            // 1. Exactly 1 hour before bedtime (within 30 mins)
            // 2. Or if active and already past bedtime (up to 4 hours past)
            double hoursDiff = bedOffset - nowOffset;
            bool shouldTrigger = (hoursDiff <= 1.05 && hoursDiff >= 0.0) ||
                                 (nowOffset >= bedOffset && nowOffset <= bedOffset + 4.0);

            if (shouldTrigger)
            {
                _lastNotifiedCycleDate = cycleDate;
                ShowRecommendationPopup(avgBedtime);
            }
        }
        catch { }
        finally
        {
            _isChecking = false;
        }
    }

    public async void TestNotification()
    {
        var avg = await GetAverageBedtimeAsync();
        ShowRecommendationPopup(avg ?? TimeSpan.FromHours(0.5)); // Default to 00:30 if empty
    }

    private void ShowRecommendationPopup(TimeSpan bedtime)
    {
        var now = DateTime.Now;
        double bedOffset = (bedtime.TotalHours - 5 + 24) % 24;
        double nowOffset = (now.TimeOfDay.TotalHours - 5 + 24) % 24;

        bool isPastBedtime = nowOffset >= bedOffset;

        string bedtimeFormatted = $"{bedtime.Hours:D2}:{bedtime.Minutes:D2}";
        string nowFormatted = $"{now.Hour:D2}:{now.Minute:D2}";

        bool isRu = _localization.CurrentLanguage == AppLanguage.Ru;
        string title = isRu ? "🌙 Рекомендация ко сну" : "🌙 Bedtime Recommendation";

        string msg;
        if (isPastBedtime)
        {
            msg = isRu
                ? $"Обычно вы ложитесь около {bedtimeFormatted}. Время уже {nowFormatted} — вы засиделись позже обычного! Рекомендуем завершать работу за ПК и отдохнуть."
                : $"Usually you go to bed around {bedtimeFormatted}. It is already {nowFormatted} — you are staying up later than usual! We recommend wrapping up your PC work and getting some rest.";
        }
        else
        {
            msg = isRu
                ? $"Обычно вы ложитесь в {bedtimeFormatted}. До сна остался 1 час, рекомендуем завершать работу за ПК."
                : $"Usually you go to bed around {bedtimeFormatted}. 1 hour left until sleep, we recommend wrapping up your PC work.";
        }

        Task.Run(() =>
        {
            NativeMethods.MessageBox(
                IntPtr.Zero,
                msg,
                title,
                NativeMethods.MB_OK |
                NativeMethods.MB_ICONINFORMATION |
                NativeMethods.MB_TOPMOST |
                NativeMethods.MB_SETFOREGROUND |
                NativeMethods.MB_SYSTEMMODAL);
        });
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}

