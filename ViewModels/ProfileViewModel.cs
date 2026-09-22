using System.Collections.ObjectModel;
using System.Windows.Input;
using WinTime.Core;
using WinTime.Data;
using WinTime.Models;
using WinTime.Services;

namespace WinTime.ViewModels;

public sealed class ProfileViewModel : BaseViewModel
{
    private readonly ActivityRepository  _activityRepo;
    private readonly ActivityTracker     _tracker;
    private readonly LocalizationService _localization;

    private int _userLevel = 1;
    private string _rankTitle = "🌱 Новичок";
    private long _currentXp;
    private long _nextLevelXp = 600;
    private double _levelProgress;
    private string _levelProgressText = "0 / 600 XP";

    private string _totalTimeFormatted = "0 ч 0 м";
    private string _activeDaysFormatted = "0 дн.";
    private string _unlockedAchievementsCountText = "0 / 12";

    private ObservableCollection<AchievementItem> _achievements = [];

    public int UserLevel
    {
        get => _userLevel;
        private set => SetProperty(ref _userLevel, value);
    }

    public string RankTitle
    {
        get => _rankTitle;
        private set => SetProperty(ref _rankTitle, value);
    }

    public long CurrentXp
    {
        get => _currentXp;
        private set => SetProperty(ref _currentXp, value);
    }

    public long NextLevelXp
    {
        get => _nextLevelXp;
        private set => SetProperty(ref _nextLevelXp, value);
    }

    public double LevelProgress
    {
        get => _levelProgress;
        private set => SetProperty(ref _levelProgress, value);
    }

    public string LevelProgressText
    {
        get => _levelProgressText;
        private set => SetProperty(ref _levelProgressText, value);
    }

    public string TotalTimeFormatted
    {
        get => _totalTimeFormatted;
        private set => SetProperty(ref _totalTimeFormatted, value);
    }

    public string ActiveDaysFormatted
    {
        get => _activeDaysFormatted;
        private set => SetProperty(ref _activeDaysFormatted, value);
    }

    public string UnlockedAchievementsCountText
    {
        get => _unlockedAchievementsCountText;
        private set => SetProperty(ref _unlockedAchievementsCountText, value);
    }

    public ObservableCollection<AchievementItem> Achievements
    {
        get => _achievements;
        private set => SetProperty(ref _achievements, value);
    }

    public ICommand RefreshCommand { get; }

    public ProfileViewModel(ActivityRepository activityRepo, ActivityTracker tracker, LocalizationService localization)
    {
        _activityRepo = activityRepo;
        _tracker      = tracker;
        _localization = localization;

        _localization.LanguageChanged += async (_, _) => await LoadAsync();
        RefreshCommand = new RelayCommand(async () => await LoadAsync());
    }

    public async Task LoadAsync()
    {
        try
        {
            await _tracker.FlushToDbAsync();

            var (lifetimeActive, maxDaySec, activeDays, totalClicks, totalDistMeters) =
                await _activityRepo.GetLifetimeStatsAsync();

            // 1 minute of activity = 10 XP (600 XP/hr)
            long totalXp = (lifetimeActive / 60) * 10;
            CurrentXp = totalXp;

            // Level progression: each level requires 1000 XP
            const int xpPerLevel = 1000;
            int level = (int)(totalXp / xpPerLevel) + 1;
            UserLevel = level;

            long xpInCurrentLevel = totalXp % xpPerLevel;
            NextLevelXp = xpPerLevel;
            LevelProgress = Math.Clamp((double)xpInCurrentLevel / xpPerLevel * 100.0, 0, 100);
            LevelProgressText = $"{xpInCurrentLevel} / {xpPerLevel} XP";

            RankTitle = GetRankTitle(level, _localization.CurrentLanguage);

            var ts = TimeSpan.FromSeconds(lifetimeActive);
            bool isEn = _localization.CurrentLanguage == AppLanguage.En;
            TotalTimeFormatted = isEn
                ? $"{(int)ts.TotalHours}h {ts.Minutes}m"
                : $"{(int)ts.TotalHours} ч {ts.Minutes} м";
            ActiveDaysFormatted = isEn
                ? $"{activeDays} d."
                : $"{activeDays} дн.";

            var list = BuildAchievements(lifetimeActive, maxDaySec, activeDays, totalClicks, totalDistMeters, isEn);
            int unlockedCount = list.Count(a => a.IsUnlocked);
            UnlockedAchievementsCountText = $"{unlockedCount} / {list.Count}";

            Achievements = new ObservableCollection<AchievementItem>(list);
        }
        catch { }
    }

    private static string GetRankTitle(int level, AppLanguage lang)
    {
        if (lang == AppLanguage.En)
        {
            return level switch
            {
                <= 2  => "🌱 Novice",
                <= 5  => "🥉 Practitioner",
                <= 10 => "🥈 Enthusiast",
                <= 20 => "🥇 Specialist",
                <= 35 => "🏆 Focus Master",
                <= 50 => "⚡ Cyber Jedi",
                _     => "👑 Grandmaster"
            };
        }

        return level switch
        {
            <= 2  => "🌱 Новичок",
            <= 5  => "🥉 Практикант",
            <= 10 => "🥈 Энтузиаст",
            <= 20 => "🥇 Специалист",
            <= 35 => "🏆 Мастер фокуса",
            <= 50 => "⚡ Кибер-джедай",
            _     => "👑 Грандмастер"
        };
    }

    private static List<AchievementItem> BuildAchievements(
        long totalSec, long maxDaySec, int activeDays, long clicks, double distMeters, bool isEn)
    {
        double totalHours = totalSec / 3600.0;
        double maxDayHours = maxDaySec / 3600.0;

        if (isEn)
        {
            return
            [
                MakeAch("time_1", "First Steps", "Spend 1 hour active on your PC", "🌱",
                    totalHours >= 1, Math.Min(100, (totalHours / 1.0) * 100), $"{Math.Min(1.0, totalHours):F1} / 1 h", isEn),

                MakeAch("time_10", "Gaining Momentum", "Accumulate 10 hours of active screen time", "⏱",
                    totalHours >= 10, Math.Min(100, (totalHours / 10.0) * 100), $"{Math.Min(10.0, totalHours):F1} / 10 h", isEn),

                MakeAch("time_50", "Experienced User", "Reach 50 hours of active PC work", "⚡",
                    totalHours >= 50, Math.Min(100, (totalHours / 50.0) * 100), $"{Math.Min(50.0, totalHours):F1} / 50 h", isEn),

                MakeAch("time_100", "Century Club", "Cross the 100 hours mark", "💯",
                    totalHours >= 100, Math.Min(100, (totalHours / 100.0) * 100), $"{Math.Min(100.0, totalHours):F1} / 100 h", isEn),

                MakeAch("day_4h", "Productive Day", "Achieve 4 hours of activity in a single day", "🔋",
                    maxDayHours >= 4, Math.Min(100, (maxDayHours / 4.0) * 100), $"{Math.Min(4.0, maxDayHours):F1} / 4 h", isEn),

                MakeAch("day_8h", "Workaholic", "Work a record 8 hours in a single calendar day", "🔥",
                    maxDayHours >= 8, Math.Min(100, (maxDayHours / 8.0) * 100), $"{Math.Min(8.0, maxDayHours):F1} / 8 h", isEn),

                MakeAch("days_3", "Three Days Streak", "Active on PC for at least 3 days", "📅",
                    activeDays >= 3, Math.Min(100, (activeDays / 3.0) * 100), $"{Math.Min(3, activeDays)} / 3 d.", isEn),

                MakeAch("days_7", "Weekly Resilience", "Maintain activity across 7 days", "🗓",
                    activeDays >= 7, Math.Min(100, (activeDays / 7.0) * 100), $"{Math.Min(7, activeDays)} / 7 d.", isEn),

                MakeAch("days_30", "Discipline Master", "Accumulate 30 days of activity", "🛡",
                    activeDays >= 30, Math.Min(100, (activeDays / 30.0) * 100), $"{Math.Min(30, activeDays)} / 30 d.", isEn),

                MakeAch("mouse_1k", "Finger Warmup", "Perform 1,000 mouse clicks", "🖱",
                    clicks >= 1000, Math.Min(100, (clicks / 1000.0) * 100), $"{Math.Min(1000, clicks):N0} / 1,000", isEn),

                MakeAch("mouse_10k", "Click Machine", "Reach 10,000 mouse clicks", "⚡",
                    clicks >= 10000, Math.Min(100, (clicks / 10000.0) * 100), $"{Math.Min(10000, clicks):N0} / 10,000", isEn),

                MakeAch("mouse_dist_1km", "Cursor Marathoner", "Travel 1 kilometer with mouse cursor", "🏃",
                    distMeters >= 1000, Math.Min(100, (distMeters / 1000.0) * 100), $"{Math.Min(1.0, distMeters / 1000.0):F2} / 1.00 km", isEn)
            ];
        }

        return
        [
            MakeAch("time_1", "Первые шаги", "Провести 1 час за компьютером в приложении", "🌱",
                totalHours >= 1, Math.Min(100, (totalHours / 1.0) * 100), $"{Math.Min(1.0, totalHours):F1} / 1 ч", isEn),

            MakeAch("time_10", "Набирая обороты", "Провести суммарно 10 часов активного времени", "⏱",
                totalHours >= 10, Math.Min(100, (totalHours / 10.0) * 100), $"{Math.Min(10.0, totalHours):F1} / 10 ч", isEn),

            MakeAch("time_50", "Опытный юзер", "Накопить 50 часов экранного времени", "⚡",
                totalHours >= 50, Math.Min(100, (totalHours / 50.0) * 100), $"{Math.Min(50.0, totalHours):F1} / 50 ч", isEn),

            MakeAch("time_100", "Сотня в кармане", "Преодолеть отметку в 100 часов за работой", "💯",
                totalHours >= 100, Math.Min(100, (totalHours / 100.0) * 100), $"{Math.Min(100.0, totalHours):F1} / 100 ч", isEn),

            MakeAch("day_4h", "Полноценный день", "Провести 4 часа активности за один день", "🔋",
                maxDayHours >= 4, Math.Min(100, (maxDayHours / 4.0) * 100), $"{Math.Min(4.0, maxDayHours):F1} / 4 ч", isEn),

            MakeAch("day_8h", "Трудоголик", "Отработать рекордные 8 часов за один календарный день", "🔥",
                maxDayHours >= 8, Math.Min(100, (maxDayHours / 8.0) * 100), $"{Math.Min(8.0, maxDayHours):F1} / 8 ч", isEn),

            MakeAch("days_3", "Три дня подряд", "Активность минимум 3 дня", "📅",
                activeDays >= 3, Math.Min(100, (activeDays / 3.0) * 100), $"{Math.Min(3, activeDays)} / 3 дн.", isEn),

            MakeAch("days_7", "Неделя стойкости", "Зафиксировать активность в течение 7 дней", "🗓",
                activeDays >= 7, Math.Min(100, (activeDays / 7.0) * 100), $"{Math.Min(7, activeDays)} / 7 дн.", isEn),

            MakeAch("days_30", "Месяц дисциплины", "Накопить 30 дней активности", "🛡",
                activeDays >= 30, Math.Min(100, (activeDays / 30.0) * 100), $"{Math.Min(30, activeDays)} / 30 дн.", isEn),

            MakeAch("mouse_1k", "Разминка пальцев", "Сделать 1,000 кликов мыши", "🖱",
                clicks >= 1000, Math.Min(100, (clicks / 1000.0) * 100), $"{Math.Min(1000, clicks):N0} / 1,000", isEn),

            MakeAch("mouse_10k", "Клик-машина", "Накопить 10,000 кликов мыши", "⚡",
                clicks >= 10000, Math.Min(100, (clicks / 10000.0) * 100), $"{Math.Min(10000, clicks):N0} / 10,000", isEn),

            MakeAch("mouse_dist_1km", "Курсор-марафонец", "Пройти курсором мыши 1 километр расстояния", "🏃",
                distMeters >= 1000, Math.Min(100, (distMeters / 1000.0) * 100), $"{Math.Min(1.0, distMeters / 1000.0):F2} / 1.00 км", isEn)
        ];
    }

    private static AchievementItem MakeAch(
        string id, string title, string desc, string icon, bool unlocked, double progress, string progressText, bool isEn)
    {
        string statusText = unlocked
            ? (isEn ? "Unlocked" : "Получено")
            : (isEn ? "In Progress" : "В процессе");

        return new AchievementItem
        {
            Id = id,
            Title = title,
            Description = desc,
            Icon = icon,
            IsUnlocked = unlocked,
            Progress = Math.Clamp(progress, 0, 100),
            ProgressText = progressText,
            StatusBadgeText = statusText
        };
    }
}
