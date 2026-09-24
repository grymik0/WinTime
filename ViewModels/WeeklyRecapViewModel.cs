using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using WinTime.Data;
using WinTime.Models;
using WinTime.Services;

namespace WinTime.ViewModels;

public enum RecapWeekMode
{
    CurrentWeek,
    PreviousWeek
}

public sealed class RecapAppItem
{
    public string RankBadge     { get; init; } = string.Empty;
    public string DisplayName   { get; init; } = string.Empty;
    public string FormattedTime { get; init; } = string.Empty;
    public double Percentage    { get; init; }
}

public sealed class WeeklyRecapViewModel : BaseViewModel
{
    private readonly ActivityRepository  _activityRepo;
    private readonly LocalizationService _localization;

    private bool   _isLoading;
    private string _periodFormatted        = string.Empty;
    private RecapWeekMode _selectedMode    = RecapWeekMode.CurrentWeek;

    // Archetype / Persona
    private string _personaBadge           = "🎯";
    private string _personaTitle           = "Исследователь времени";
    private string _personaDesc            = "Вы активно изучаете свои цифровые привычки.";
    private string _personaColor           = "#6366F1";

    // Screen Time & Daily average
    private string _totalActiveFormatted   = "0ч 0м";
    private string _dailyAvgFormatted      = string.Empty;
    private string _trendFormatted         = string.Empty;
    private bool   _isTrendUp;
    private bool   _hasTrend;

    // Breakthrough & Cooldown apps
    private bool   _hasBreakthrough;
    private string _breakthroughApp        = string.Empty;
    private string _breakthroughDiff       = string.Empty;

    private bool   _hasCooldown;
    private string _cooldownApp           = string.Empty;
    private string _cooldownDiff          = string.Empty;

    // Workdays vs Weekends
    private string _workdaysFormatted      = "0ч 0м (0%)";
    private double _workdaysPercentage     = 50;
    private string _weekendsFormatted      = "0ч 0м (0%)";
    private double _weekendsPercentage     = 50;
    private string _workVsWeekendVerdict   = string.Empty;

    // Time of day golden slot
    private string _peakTimeSlotTitle      = "—";
    private string _peakTimeSlotFormatted  = "—";

    // Top Apps
    public ObservableCollection<RecapAppItem> TopApps { get; } = new();

    // Sleep & Peak Day
    private string _peakDayName            = "—";
    private string _peakDayTimeFormatted   = "—";
    private string _avgBedtime             = "—";
    private string _avgWakeup              = "—";
    private string _avgRestDuration        = "—";

    // Mouse metrics
    private string _totalClicksFormatted   = "0";
    private string _totalDistanceFormatted = "0 м";
    private string _funFactText            = string.Empty;

    // Copy Feedback
    private string _copyFeedbackText       = string.Empty;

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public RecapWeekMode SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (SetProperty(ref _selectedMode, value))
            {
                OnPropertyChanged(nameof(IsCurrentWeek));
                OnPropertyChanged(nameof(IsPreviousWeek));
                _ = LoadAsync();
            }
        }
    }

    public bool IsCurrentWeek => _selectedMode == RecapWeekMode.CurrentWeek;
    public bool IsPreviousWeek => _selectedMode == RecapWeekMode.PreviousWeek;

    public ICommand SelectCurrentWeekCommand { get; }
    public ICommand SelectPreviousWeekCommand { get; }
    public ICommand CopyDigestCommand { get; }

    public string PeriodFormatted
    {
        get => _periodFormatted;
        private set => SetProperty(ref _periodFormatted, value);
    }

    public string PersonaBadge
    {
        get => _personaBadge;
        private set => SetProperty(ref _personaBadge, value);
    }

    public string PersonaTitle
    {
        get => _personaTitle;
        private set => SetProperty(ref _personaTitle, value);
    }

    public string PersonaDesc
    {
        get => _personaDesc;
        private set => SetProperty(ref _personaDesc, value);
    }

    public string PersonaColor
    {
        get => _personaColor;
        private set => SetProperty(ref _personaColor, value);
    }

    public string TotalActiveFormatted
    {
        get => _totalActiveFormatted;
        private set => SetProperty(ref _totalActiveFormatted, value);
    }

    public string DailyAvgFormatted
    {
        get => _dailyAvgFormatted;
        private set => SetProperty(ref _dailyAvgFormatted, value);
    }

    public string TrendFormatted
    {
        get => _trendFormatted;
        private set => SetProperty(ref _trendFormatted, value);
    }

    public bool IsTrendUp
    {
        get => _isTrendUp;
        private set => SetProperty(ref _isTrendUp, value);
    }

    public bool HasTrend
    {
        get => _hasTrend;
        private set => SetProperty(ref _hasTrend, value);
    }

    public bool HasBreakthrough
    {
        get => _hasBreakthrough;
        private set => SetProperty(ref _hasBreakthrough, value);
    }

    public string BreakthroughApp
    {
        get => _breakthroughApp;
        private set => SetProperty(ref _breakthroughApp, value);
    }

    public string BreakthroughDiff
    {
        get => _breakthroughDiff;
        private set => SetProperty(ref _breakthroughDiff, value);
    }

    public bool HasCooldown
    {
        get => _hasCooldown;
        private set => SetProperty(ref _hasCooldown, value);
    }

    public string CooldownApp
    {
        get => _cooldownApp;
        private set => SetProperty(ref _cooldownApp, value);
    }

    public string CooldownDiff
    {
        get => _cooldownDiff;
        private set => SetProperty(ref _cooldownDiff, value);
    }

    public string WorkdaysFormatted
    {
        get => _workdaysFormatted;
        private set => SetProperty(ref _workdaysFormatted, value);
    }

    public double WorkdaysPercentage
    {
        get => _workdaysPercentage;
        private set => SetProperty(ref _workdaysPercentage, value);
    }

    public string WeekendsFormatted
    {
        get => _weekendsFormatted;
        private set => SetProperty(ref _weekendsFormatted, value);
    }

    public double WeekendsPercentage
    {
        get => _weekendsPercentage;
        private set => SetProperty(ref _weekendsPercentage, value);
    }

    public string WorkVsWeekendVerdict
    {
        get => _workVsWeekendVerdict;
        private set => SetProperty(ref _workVsWeekendVerdict, value);
    }

    public string PeakTimeSlotTitle
    {
        get => _peakTimeSlotTitle;
        private set => SetProperty(ref _peakTimeSlotTitle, value);
    }

    public string PeakTimeSlotFormatted
    {
        get => _peakTimeSlotFormatted;
        private set => SetProperty(ref _peakTimeSlotFormatted, value);
    }

    public string PeakDayName
    {
        get => _peakDayName;
        private set => SetProperty(ref _peakDayName, value);
    }

    public string PeakDayTimeFormatted
    {
        get => _peakDayTimeFormatted;
        private set => SetProperty(ref _peakDayTimeFormatted, value);
    }

    public string AvgBedtime
    {
        get => _avgBedtime;
        private set => SetProperty(ref _avgBedtime, value);
    }

    public string AvgWakeup
    {
        get => _avgWakeup;
        private set => SetProperty(ref _avgWakeup, value);
    }

    public string AvgRestDuration
    {
        get => _avgRestDuration;
        private set => SetProperty(ref _avgRestDuration, value);
    }

    public string TotalClicksFormatted
    {
        get => _totalClicksFormatted;
        private set => SetProperty(ref _totalClicksFormatted, value);
    }

    public string TotalDistanceFormatted
    {
        get => _totalDistanceFormatted;
        private set => SetProperty(ref _totalDistanceFormatted, value);
    }

    public string FunFactText
    {
        get => _funFactText;
        private set => SetProperty(ref _funFactText, value);
    }

    public string CopyFeedbackText
    {
        get => _copyFeedbackText;
        private set => SetProperty(ref _copyFeedbackText, value);
    }

    public WeeklyRecapViewModel(
        ActivityRepository activityRepo,
        LocalizationService localization)
    {
        _activityRepo = activityRepo;
        _localization = localization;

        SelectCurrentWeekCommand = new RelayCommand(() => SelectedMode = RecapWeekMode.CurrentWeek);
        SelectPreviousWeekCommand = new RelayCommand(() => SelectedMode = RecapWeekMode.PreviousWeek);
        CopyDigestCommand = new RelayCommand(CopyDigestToClipboard);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var today = DateTime.Today;
            int currentDayOfWeek = (int)today.DayOfWeek;
            int daysSinceMonday = currentDayOfWeek == 0 ? 6 : currentDayOfWeek - 1;
            DateTime currentMonday = today.AddDays(-daysSinceMonday);

            DateTime from, to, prevFrom, prevTo;
            DateTime recapEndDisplay;

            var isRu = _localization.CurrentLanguage == AppLanguage.Ru;
            var cult = isRu ? new CultureInfo("ru-RU") : CultureInfo.InvariantCulture;

            if (_selectedMode == RecapWeekMode.CurrentWeek)
            {
                from = currentMonday;
                to = today.AddDays(1);
                prevFrom = currentMonday.AddDays(-7);
                prevTo = currentMonday;
                recapEndDisplay = today;

                PeriodFormatted = $"{from.ToString("d MMMM", cult)} — {today.ToString("d MMMM yyyy", cult)}";
            }
            else
            {
                from = currentMonday.AddDays(-7);
                to = currentMonday;
                prevFrom = currentMonday.AddDays(-14);
                prevTo = currentMonday.AddDays(-7);
                recapEndDisplay = from.AddDays(6); // Sunday

                PeriodFormatted = $"{from.ToString("d MMMM", cult)} — {recapEndDisplay.ToString("d MMMM yyyy", cult)}";
            }

            // 1. Total screen time & daily average
            var (activeSec, idleSec) = await _activityRepo.GetTotalsAsync(from, to);
            TotalActiveFormatted = LocalizationService.FormatDuration(activeSec);

            int daysElapsed = _selectedMode == RecapWeekMode.CurrentWeek ? Math.Max(1, daysSinceMonday + 1) : 7;
            long dailyAvgSec = activeSec / daysElapsed;
            DailyAvgFormatted = $"~{LocalizationService.FormatDuration(dailyAvgSec)} {(isRu ? "в день" : "per day")}";

            // Previous period comparison
            var (prevActiveSec, _) = await _activityRepo.GetTotalsAsync(prevFrom, prevTo);

            if (prevActiveSec > 0)
            {
                double pct = (double)(activeSec - prevActiveSec) / prevActiveSec * 100.0;
                HasTrend = true;
                string compPeriodWord = _selectedMode == RecapWeekMode.CurrentWeek
                    ? (isRu ? "прошлой недели" : "last week")
                    : (isRu ? "позапрошлой недели" : "two weeks ago");

                if (pct > 0)
                {
                    IsTrendUp = true;
                    TrendFormatted = isRu
                        ? $"+{pct:F0}% больше {compPeriodWord}"
                        : $"+{pct:F0}% vs {compPeriodWord}";
                }
                else if (pct < 0)
                {
                    IsTrendUp = false;
                    TrendFormatted = isRu
                        ? $"{pct:F0}% меньше {compPeriodWord}"
                        : $"{pct:F0}% vs {compPeriodWord}";
                }
                else
                {
                    IsTrendUp = false;
                    TrendFormatted = isRu ? "На том же уровне" : "Same level";
                }
            }
            else
            {
                HasTrend = false;
                TrendFormatted = isRu ? "Первая неделя активности" : "First tracked week";
            }

            // 2. Top 3 Apps
            var curTopApps = await _activityRepo.GetTopAppsAsync(from, to);
            var prevTopApps = await _activityRepo.GetTopAppsAsync(prevFrom, prevTo);

            var top3 = curTopApps.Take(3).ToList();
            var recapApps = new List<RecapAppItem>();
            string[] badges = ["🥇", "🥈", "🥉"];
            int idx = 0;
            foreach (var app in top3)
            {
                double pct = activeSec > 0 ? (double)app.TotalSeconds / activeSec * 100.0 : 0;
                recapApps.Add(new RecapAppItem
                {
                    RankBadge = idx < badges.Length ? badges[idx] : $"#{idx + 1}",
                    DisplayName = !string.IsNullOrWhiteSpace(app.DisplayName) ? app.DisplayName : app.ProcessName,
                    FormattedTime = LocalizationService.FormatDuration(app.TotalSeconds),
                    Percentage = pct
                });
                idx++;
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                await dispatcher.InvokeAsync(() =>
                {
                    TopApps.Clear();
                    foreach (var it in recapApps) TopApps.Add(it);
                });
            }
            else
            {
                TopApps.Clear();
                foreach (var it in recapApps) TopApps.Add(it);
            }

            // 3. Breakthrough & Cooldown apps
            var prevDict = prevTopApps.ToDictionary(a => a.ProcessName.ToLowerInvariant(), a => a.TotalSeconds);
            var growthList = new List<(string Name, long DiffSec)>();
            foreach (var app in curTopApps)
            {
                long prevSec = prevDict.GetValueOrDefault(app.ProcessName.ToLowerInvariant(), 0L);
                long diff = app.TotalSeconds - prevSec;
                string name = !string.IsNullOrWhiteSpace(app.DisplayName) ? app.DisplayName : app.ProcessName;
                growthList.Add((name, diff));
            }

            var curDict = curTopApps.ToDictionary(a => a.ProcessName.ToLowerInvariant(), a => a.TotalSeconds);
            foreach (var prevApp in prevTopApps)
            {
                if (!curDict.ContainsKey(prevApp.ProcessName.ToLowerInvariant()))
                {
                    string name = !string.IsNullOrWhiteSpace(prevApp.DisplayName) ? prevApp.DisplayName : prevApp.ProcessName;
                    growthList.Add((name, -prevApp.TotalSeconds));
                }
            }

            var topGrower = growthList.Where(g => g.DiffSec > 0).OrderByDescending(g => g.DiffSec).FirstOrDefault();
            if (topGrower.DiffSec > 0)
            {
                HasBreakthrough = true;
                BreakthroughApp = topGrower.Name;
                BreakthroughDiff = $"+{LocalizationService.FormatDuration(topGrower.DiffSec)} {(isRu ? "к прошлой неделе" : "vs last week")}";
            }
            else
            {
                HasBreakthrough = false;
                BreakthroughApp = isRu ? "Без резкого роста" : "No spikes";
                BreakthroughDiff = isRu ? "Равномерная активность" : "Even activity";
            }

            var topDropper = growthList.Where(g => g.DiffSec < 0).OrderBy(g => g.DiffSec).FirstOrDefault();
            if (topDropper.DiffSec < 0)
            {
                HasCooldown = true;
                CooldownApp = topDropper.Name;
                CooldownDiff = $"-{LocalizationService.FormatDuration(Math.Abs(topDropper.DiffSec))} {(isRu ? "к прошлой неделе" : "vs last week")}";
            }
            else
            {
                HasCooldown = false;
                CooldownApp = isRu ? "Без спада" : "No decline";
                CooldownDiff = prevTopApps.Count == 0
                    ? (isRu ? "Первая неделя отслеживания" : "First tracked week")
                    : (isRu ? "Все приложения в плюсе 📈" : "All apps increased 📈");
            }

            // 4. Workdays vs Weekends breakdown
            var history = await _activityRepo.GetDailyActivityHistoryAsync(from);
            long workdaysSec = 0;
            long weekendsSec = 0;
            DateTime bestDate = DateTime.MinValue;
            long bestSec = 0;

            for (var d = from; d <= recapEndDisplay; d = d.AddDays(1))
            {
                string key = d.ToString("yyyy-MM-dd");
                long sec = history.GetValueOrDefault(key, 0L);

                if (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday)
                    weekendsSec += sec;
                else
                    workdaysSec += sec;

                if (sec > bestSec)
                {
                    bestSec = sec;
                    bestDate = d;
                }
            }

            long totalDaysSec = workdaysSec + weekendsSec;
            if (totalDaysSec > 0)
            {
                WorkdaysPercentage = Math.Round((double)workdaysSec / totalDaysSec * 100.0, 1);
                WeekendsPercentage = Math.Round((double)weekendsSec / totalDaysSec * 100.0, 1);
                WorkdaysFormatted = $"{LocalizationService.FormatDuration(workdaysSec)} ({WorkdaysPercentage:F0}%)";
                WeekendsFormatted = $"{LocalizationService.FormatDuration(weekendsSec)} ({WeekendsPercentage:F0}%)";

                if (WeekendsPercentage <= 20)
                    WorkVsWeekendVerdict = isRu ? "🌿 Отличный цифровой детокс на выходных!" : "🌿 Great digital detox on weekends!";
                else if (WeekendsPercentage >= 45)
                    WorkVsWeekendVerdict = isRu ? "🎮 Выходные прошли в активном погружении за ПК" : "🎮 Highly active weekend screen sessions";
                else
                    WorkVsWeekendVerdict = isRu ? "⚖️ Сбалансированное распределение между буднями и отдыхом" : "⚖️ Balanced workday and weekend usage";
            }
            else
            {
                WorkdaysFormatted = "0ч (0%)";
                WeekendsFormatted = "0ч (0%)";
                WorkVsWeekendVerdict = "—";
            }

            // Peak Day
            if (bestSec > 0)
            {
                PeakDayName = bestDate.ToString("dddd, d MMMM", cult);
                PeakDayTimeFormatted = LocalizationService.FormatDuration(bestSec);
            }
            else
            {
                PeakDayName = "—";
                PeakDayTimeFormatted = "—";
            }

            // 5. Time of Day breakdown (Golden Hours)
            var (nightSec, morningSec, afternoonSec, eveningSec) = await _activityRepo.GetTimeOfDayBreakdownAsync(from, to);
            long maxSlotSec = Math.Max(nightSec, Math.Max(morningSec, Math.Max(afternoonSec, eveningSec)));

            if (maxSlotSec > 0 && activeSec > 0)
            {
                double slotPct = (double)maxSlotSec / activeSec * 100.0;
                string slotTime = LocalizationService.FormatDuration(maxSlotSec);

                if (maxSlotSec == nightSec)
                {
                    PeakTimeSlotTitle = isRu ? "🌙 Ночь (00:00 — 06:00)" : "🌙 Night (00:00 — 06:00)";
                    PeakTimeSlotFormatted = $"{slotTime} · {slotPct:F0}% {(isRu ? "времени" : "of time")}";
                }
                else if (maxSlotSec == morningSec)
                {
                    PeakTimeSlotTitle = isRu ? "🌅 Утро (06:00 — 12:00)" : "🌅 Morning (06:00 — 12:00)";
                    PeakTimeSlotFormatted = $"{slotTime} · {slotPct:F0}% {(isRu ? "времени" : "of time")}";
                }
                else if (maxSlotSec == afternoonSec)
                {
                    PeakTimeSlotTitle = isRu ? "☀️ День (12:00 — 18:00)" : "☀️ Afternoon (12:00 — 18:00)";
                    PeakTimeSlotFormatted = $"{slotTime} · {slotPct:F0}% {(isRu ? "времени" : "of time")}";
                }
                else
                {
                    PeakTimeSlotTitle = isRu ? "🌆 Вечер (18:00 — 00:00)" : "🌆 Evening (18:00 — 00:00)";
                    PeakTimeSlotFormatted = $"{slotTime} · {slotPct:F0}% {(isRu ? "времени" : "of time")}";
                }
            }
            else
            {
                PeakTimeSlotTitle = "—";
                PeakTimeSlotFormatted = "—";
            }

            // 6. Sleep & Rest Rhythm
            int rhythmDays = _selectedMode == RecapWeekMode.CurrentWeek ? Math.Max(1, daysSinceMonday + 1) : 7;
            var rhythms = await _activityRepo.GetDailyRhythmsAsync(rhythmDays);
            TimeSpan avgBed = TimeSpan.Zero;
            TimeSpan avgWake = TimeSpan.Zero;
            if (rhythms.Count > 0)
            {
                avgBed = BedtimeReminderService.CalculateCircularAverageTime(rhythms.Select(r => r.LastActive));
                avgWake = BedtimeReminderService.CalculateCircularAverageTime(rhythms.Select(r => r.FirstActive));
                AvgBedtime = $"~{avgBed.Hours:D2}:{avgBed.Minutes:D2}";
                AvgWakeup = $"~{avgWake.Hours:D2}:{avgWake.Minutes:D2}";

                var restList = new List<double>();
                for (int i = 0; i < rhythms.Count - 1; i++)
                {
                    var cur = rhythms[i];
                    var nxt = rhythms[i + 1];
                    var diffH = (nxt.Date.Add(nxt.FirstActive) - cur.Date.Add(cur.LastActive)).TotalHours;
                    if (diffH >= 2 && diffH <= 20)
                        restList.Add(diffH);
                }
                if (restList.Count > 0)
                {
                    double avgH = restList.Average();
                    int rh = (int)avgH;
                    int rm = (int)((avgH - rh) * 60);
                    AvgRestDuration = isRu ? $"{rh}ч {rm:D2}м" : $"{rh}h {rm:D2}m";
                }
                else
                {
                    AvgRestDuration = "—";
                }
            }
            else
            {
                AvgBedtime = "—";
                AvgWakeup = "—";
                AvgRestDuration = "—";
            }

            // 7. Mouse Metrics & Fun Fact
            var (clicks, distMeters) = await _activityRepo.GetDailyMetricsAsync(from, to);
            TotalClicksFormatted = clicks.ToString("N0", cult);
            TotalDistanceFormatted = distMeters >= 1000
                ? $"{distMeters / 1000.0:F2} км"
                : $"{distMeters:F0} м";

            if (distMeters > 0)
            {
                double km = distMeters / 1000.0;
                int footballFields = Math.Max(1, (int)Math.Round(distMeters / 105.0));
                FunFactText = isRu
                    ? $"💡 Курсор преодолел {km:F2} км — это длина {footballFields} футбольных полей!"
                    : $"💡 Your cursor traveled {km:F2} km — equal to {footballFields} football fields!";
            }
            else
            {
                FunFactText = isRu
                    ? "💡 Двигайте мышью активнее, чтобы открыть интересные факты!"
                    : "💡 Move your mouse to discover fun real-world distance stats!";
            }

            // 8. Weekly Persona Archetype calculation
            DeterminePersona(activeSec, idleSec, clicks, nightSec, eveningSec, avgBed, top3.FirstOrDefault(), isRu);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WeeklyRecapViewModel error: {ex}");
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void DeterminePersona(
        long activeSec,
        long idleSec,
        long clicks,
        long nightSec,
        long eveningSec,
        TimeSpan avgBedtime,
        AppStatItem? topApp,
        bool isRu)
    {
        bool isNightOwl = (nightSec > 0 && (double)nightSec / Math.Max(1, activeSec) > 0.25) ||
                          (avgBedtime.Hours >= 1 && avgBedtime.Hours < 6);

        bool isMarathoner = activeSec >= 35 * 3600;
        bool isClickSniper = clicks >= 12000;

        long totalSpan = activeSec + idleSec;
        bool isTurboFocus = activeSec >= 10 * 3600 && totalSpan > 0 && ((double)idleSec / totalSpan) < 0.10;

        bool isGamer = topApp != null && (
            topApp.DisplayName.Contains("Steam", StringComparison.OrdinalIgnoreCase) ||
            topApp.DisplayName.Contains("Game", StringComparison.OrdinalIgnoreCase) ||
            topApp.DisplayName.Contains("Dota", StringComparison.OrdinalIgnoreCase) ||
            topApp.DisplayName.Contains("CS2", StringComparison.OrdinalIgnoreCase) ||
            topApp.DisplayName.Contains("Minecraft", StringComparison.OrdinalIgnoreCase) ||
            topApp.ProcessName.Contains("Steam", StringComparison.OrdinalIgnoreCase) ||
            topApp.ProcessName.Contains("Game", StringComparison.OrdinalIgnoreCase));

        if (isNightOwl)
        {
            PersonaBadge = "🦉";
            PersonaTitle = isRu ? "Ночной филин" : "Night Owl";
            PersonaDesc = isRu
                ? "Глубокая ночь — ваше время пиковой концентрации. Вы продуктивны, когда весь город спит."
                : "Late night is your prime focus zone. You thrive when the rest of the world is asleep.";
            PersonaColor = "#8B5CF6";
        }
        else if (isMarathoner)
        {
            PersonaBadge = "⚡";
            PersonaTitle = isRu ? "Цифровой марафонец" : "Digital Marathoner";
            PersonaDesc = isRu
                ? $"Более 35 часов за ПК за неделю! Колоссальная выносливость и верность своему делу."
                : $"Over 35 hours logged this week! Immense stamina and dedication.";
            PersonaColor = "#EC4899";
        }
        else if (isTurboFocus)
        {
            PersonaBadge = "🎯";
            PersonaTitle = isRu ? "Мастер фокуса" : "Focus Champion";
            PersonaDesc = isRu
                ? "Меньше 10% времени ушло в бездействие. Вы садитесь за ПК с чёткой целью и не отвлекаетесь."
                : "Less than 10% idle ratio. Laser focus with almost zero wasted distraction.";
            PersonaColor = "#10B981";
        }
        else if (isGamer)
        {
            PersonaBadge = "🎮";
            PersonaTitle = isRu ? "Повелитель миров" : "Gaming Legend";
            PersonaDesc = isRu
                ? $"Главная часть недели посвящена виртуальным мирам и покорению игровых вершин."
                : $"Virtual adventures and games claimed the spotlight this week.";
            PersonaColor = "#F59E0B";
        }
        else if (isClickSniper)
        {
            PersonaBadge = "🖱️";
            PersonaTitle = isRu ? "Снайпер кликов" : "Click Maestro";
            PersonaDesc = isRu
                ? $"Свыше {clicks:N0} кликов за неделю! Ваши пальцы и мышь работают со скоростью мысли."
                : $"Over {clicks:N0} clicks registered! Lightning-fast precision interactions.";
            PersonaColor = "#06B6D4";
        }
        else
        {
            PersonaBadge = "🧘";
            PersonaTitle = isRu ? "Дзен-баланс" : "Zen Balance";
            PersonaDesc = isRu
                ? "Умеренное и стабильное экранное время без перегрузок. Гармония работы и отдыха."
                : "Harmonious screen habits with balanced pacing and healthy rest.";
            PersonaColor = "#6366F1";
        }
    }

    private async void CopyDigestToClipboard()
    {
        try
        {
            bool isRu = _localization.CurrentLanguage == AppLanguage.Ru;
            var topAppName = TopApps.FirstOrDefault()?.DisplayName ?? "—";
            var topAppTime = TopApps.FirstOrDefault()?.FormattedTime ?? "—";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"✨ {(isRu ? "Еженедельный дайджест WinTime" : "WinTime Weekly Recap")} ({PeriodFormatted})");
            sb.AppendLine($"🏆 {(isRu ? "Архетип" : "Persona")}: {PersonaBadge} {PersonaTitle}");
            sb.AppendLine($"⏱ {(isRu ? "Экранное время" : "Screen Time")}: {TotalActiveFormatted} ({DailyAvgFormatted})");
            if (HasBreakthrough)
                sb.AppendLine($"🚀 {(isRu ? "Прорыв недели" : "Breakthrough")}: {BreakthroughApp} ({BreakthroughDiff})");
            sb.AppendLine($"🥇 {(isRu ? "Топ-1 приложение" : "Top App")}: {topAppName} ({topAppTime})");
            sb.AppendLine($"⚖️ {(isRu ? "Будни / Выходные" : "Workdays / Weekends")}: {WorkdaysPercentage:F0}% / {WeekendsPercentage:F0}%");
            sb.AppendLine($"⏰ {(isRu ? "Пик активности" : "Peak Hours")}: {PeakTimeSlotTitle}");
            sb.AppendLine($"🖱 {(isRu ? "Мышь" : "Mouse")}: {TotalClicksFormatted} {(isRu ? "кликов" : "clicks")}, {TotalDistanceFormatted}");

            Clipboard.SetText(sb.ToString());

            CopyFeedbackText = isRu ? "✓ Скопировано в буфер!" : "✓ Copied to clipboard!";
            await Task.Delay(2500);
            CopyFeedbackText = string.Empty;
        }
        catch { }
    }
}

