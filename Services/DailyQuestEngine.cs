using Dapper;
using WinTime.Data;
using WinTime.Models;

namespace WinTime.Services;

public sealed class DailyQuestEngine
{
    private readonly DatabaseService     _db;
    private readonly QuestRepository    _questRepo;
    private readonly ActivityRepository _activityRepo;
    private readonly GoalRepository     _goalRepo;
    private readonly LocalizationService _localization;

    public DailyQuestEngine(
        DatabaseService db,
        QuestRepository questRepo,
        ActivityRepository activityRepo,
        GoalRepository goalRepo,
        LocalizationService localization)
    {
        _db           = db;
        _questRepo    = questRepo;
        _activityRepo = activityRepo;
        _goalRepo     = goalRepo;
        _localization = localization;
    }

    public async Task<List<DailyQuest>> EnsureAndEvaluateTodayQuestsAsync()
    {
        string todayStr = DateTime.Today.ToString("yyyy-MM-dd");
        var existing = await _questRepo.GetQuestsForDateAsync(todayStr);

        bool isRu = _localization.CurrentLanguage == AppLanguage.Ru;

        if (existing.Count == 0)
        {
            // Pick 3 deterministic quests for today using a stable day seed
            int seed = DateTime.Today.Year * 1000 + DateTime.Today.DayOfYear;
            var catalog = GetQuestCatalog(isRu);
            var rng = new Random(seed);
            var chosen = catalog.OrderBy(_ => rng.Next()).Take(3).ToList();

            foreach (var q in chosen)
            {
                q.Date = todayStr;
            }

            await _questRepo.InsertQuestsAsync(chosen);
            existing = await _questRepo.GetQuestsForDateAsync(todayStr);
        }
        else
        {
            // Update titles/descriptions to match current language if changed
            var catalog = GetQuestCatalog(isRu).ToDictionary(c => c.QuestType);
            foreach (var q in existing)
            {
                if (catalog.TryGetValue(q.QuestType, out var def))
                {
                    q.Title = def.Title;
                    q.Description = def.Description;
                    q.Icon = def.Icon;
                }
            }
        }

        // Query metrics for today
        var (earliestStart, maxSessionSec, totalTodaySec, distinctApps, clicksToday, limitsViolated) = 
            await QueryTodayMetricsAsync(todayStr);

        foreach (var q in existing)
        {
            int curVal = q.CurrentValue;
            bool isComp = q.IsCompleted;
            string progText = "";

            switch (q.QuestType)
            {
                case "early_bird":
                    // Started active session before 10:00:00
                    bool isEarly = earliestStart.HasValue && earliestStart.Value.TimeOfDay < TimeSpan.FromHours(10);
                    curVal = isEarly ? 1 : 0;
                    isComp = isEarly;
                    progText = isEarly 
                        ? (isRu ? "Зачтено (до 10:00)" : "Achieved (before 10 AM)") 
                        : (isRu ? "0 / 1 раз" : "0 / 1 time");
                    break;

                case "deep_focus":
                    // Continuous session without AFK (target in seconds, e.g. 2400s = 40m)
                    curVal = Math.Min(q.TargetValue, (int)maxSessionSec);
                    isComp = maxSessionSec >= q.TargetValue;
                    int maxMins = (int)(maxSessionSec / 60);
                    int targetMins = q.TargetValue / 60;
                    progText = isRu 
                        ? $"{Math.Min(maxMins, targetMins)} / {targetMins} мин" 
                        : $"{Math.Min(maxMins, targetMins)} / {targetMins} min";
                    break;

                case "screen_time":
                    // Total active screen time today (target 7200s = 2h)
                    curVal = Math.Min(q.TargetValue, (int)totalTodaySec);
                    isComp = totalTodaySec >= q.TargetValue;
                    int curHours = (int)(totalTodaySec / 3600);
                    int curMinRemainder = (int)((totalTodaySec % 3600) / 60);
                    progText = isRu
                        ? $"{curHours}ч {curMinRemainder}м / {q.TargetValue / 3600}ч"
                        : $"{curHours}h {curMinRemainder}m / {q.TargetValue / 3600}h";
                    break;

                case "variety":
                    // 3+ apps used today
                    curVal = Math.Min(q.TargetValue, distinctApps);
                    isComp = distinctApps >= q.TargetValue;
                    progText = isRu
                        ? $"{Math.Min(distinctApps, q.TargetValue)} / {q.TargetValue} прил."
                        : $"{Math.Min(distinctApps, q.TargetValue)} / {q.TargetValue} apps";
                    break;

                case "mouse_clicks":
                    // 500+ mouse clicks today
                    curVal = Math.Min(q.TargetValue, (int)clicksToday);
                    isComp = clicksToday >= q.TargetValue;
                    progText = isRu
                        ? $"{Math.Min(clicksToday, q.TargetValue):N0} / {q.TargetValue:N0} кл."
                        : $"{Math.Min(clicksToday, q.TargetValue):N0} / {q.TargetValue:N0} clicks";
                    break;

                case "discipline":
                    // No limits violated
                    bool clean = !limitsViolated && totalTodaySec > 0;
                    curVal = clean ? 1 : 0;
                    isComp = clean;
                    progText = limitsViolated
                        ? (isRu ? "Лимит превышен" : "Limit exceeded")
                        : (clean 
                            ? (isRu ? "Лимиты в норме ✓" : "Limits on track ✓") 
                            : (isRu ? "В процессе" : "In progress"));
                    break;
            }

            q.CurrentValue = curVal;
            q.IsCompleted = q.IsCompleted || isComp;
            q.ProgressText = progText;

            await _questRepo.UpdateQuestProgressAsync(q.Id, q.CurrentValue, q.IsCompleted);
        }

        return existing;
    }

    public async Task<UserStreakInfo> CalculateStreakAsync()
    {
        var history = await _activityRepo.GetDailyActivityHistoryAsync(DateTime.Today.AddDays(-365));
        var stored = await _questRepo.GetUserStreakAsync();

        string todayStr = DateTime.Today.ToString("yyyy-MM-dd");
        string yesterdayStr = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");

        const long activeThresholdSec = 300; // 5 minutes to qualify as active day

        bool isActiveToday = history.TryGetValue(todayStr, out long todaySec) && todaySec >= activeThresholdSec;
        bool isActiveYesterday = history.TryGetValue(yesterdayStr, out long yestSec) && yestSec >= activeThresholdSec;

        int currentStreak = 0;
        if (isActiveToday)
        {
            currentStreak = 1;
            for (int d = 1; d <= 365; d++)
            {
                string dt = DateTime.Today.AddDays(-d).ToString("yyyy-MM-dd");
                if (history.TryGetValue(dt, out long s) && s >= activeThresholdSec)
                    currentStreak++;
                else
                    break;
            }
        }
        else if (isActiveYesterday)
        {
            // Streak from yesterday is still alive, pending activity today
            currentStreak = 1;
            for (int d = 2; d <= 365; d++)
            {
                string dt = DateTime.Today.AddDays(-d).ToString("yyyy-MM-dd");
                if (history.TryGetValue(dt, out long s) && s >= activeThresholdSec)
                    currentStreak++;
                else
                    break;
            }
        }

        // Calculate all-time max streak across history
        int maxRun = 0;
        int currentRun = 0;
        DateTime? prevDate = null;

        var sortedDates = history.Where(kvp => kvp.Value >= activeThresholdSec)
                                 .Select(kvp => DateTime.TryParse(kvp.Key, out var d) ? d : (DateTime?)null)
                                 .Where(d => d.HasValue)
                                 .Select(d => d!.Value)
                                 .OrderBy(d => d)
                                 .ToList();

        foreach (var d in sortedDates)
        {
            if (prevDate.HasValue && d == prevDate.Value.AddDays(1))
            {
                currentRun++;
            }
            else
            {
                currentRun = 1;
            }
            if (currentRun > maxRun) maxRun = currentRun;
            prevDate = d;
        }

        int bestStreak = Math.Max(maxRun, Math.Max(currentStreak, stored.BestStreak));

        string? lastActive = isActiveToday ? todayStr : (isActiveYesterday ? yesterdayStr : stored.LastActiveDate);
        await _questRepo.UpdateStreakAsync(currentStreak, bestStreak, lastActive);

        return new UserStreakInfo
        {
            CurrentStreak  = currentStreak,
            BestStreak     = bestStreak,
            LastActiveDate = lastActive,
            BonusXp        = stored.BonusXp,
            IsActiveToday  = isActiveToday
        };
    }

    public async Task<bool> ClaimRewardAsync(DailyQuest quest)
    {
        if (!quest.CanClaim) return false;

        bool claimed = await _questRepo.ClaimQuestRewardAsync(quest.Id, quest.XpReward);
        if (claimed)
        {
            quest.IsClaimed = true;
        }
        return claimed;
    }

    private async Task<(DateTime? EarliestStart, long MaxSessionSec, long TotalTodaySec, int DistinctApps, long ClicksToday, bool LimitsViolated)>
        QueryTodayMetricsAsync(string todayStr)
    {
        DateTime? earliestStart = null;
        long maxSessionSec = 0;
        long totalTodaySec = 0;
        int distinctApps = 0;
        long clicksToday = 0;
        bool limitsViolated = false;

        try
        {
            var earliestStr = await _db.Connection.ExecuteScalarAsync<string?>(@"
                SELECT MIN(StartTime)
                FROM ActivitySessions
                WHERE date(StartTime) = @Date AND IsIdle = 0",
                new { Date = todayStr });

            if (!string.IsNullOrEmpty(earliestStr) && DateTime.TryParse(earliestStr, out var es))
            {
                earliestStart = es;
            }

            maxSessionSec = await _db.Connection.ExecuteScalarAsync<long>(@"
                SELECT COALESCE(MAX(DurationSeconds), 0)
                FROM ActivitySessions
                WHERE date(StartTime) = @Date AND IsIdle = 0",
                new { Date = todayStr });

            totalTodaySec = await _db.Connection.ExecuteScalarAsync<long>(@"
                SELECT COALESCE(SUM(DurationSeconds), 0)
                FROM ActivitySessions
                WHERE date(StartTime) = @Date AND IsIdle = 0",
                new { Date = todayStr });

            distinctApps = await _db.Connection.ExecuteScalarAsync<int>(@"
                SELECT COUNT(DISTINCT AppId)
                FROM ActivitySessions
                WHERE date(StartTime) = @Date AND IsIdle = 0 AND DurationSeconds >= 60",
                new { Date = todayStr });

            clicksToday = await _db.Connection.ExecuteScalarAsync<long>(@"
                SELECT COALESCE(MouseClicks, 0)
                FROM DailyMetrics
                WHERE Date = @Date",
                new { Date = todayStr });

            var limits = await _goalRepo.GetAllLimitsAsync();
            if (limits.Count > 0 && limits.Any(l => l.IsEnabled))
            {
                var today = DateTime.Today;
                var totals = await _activityRepo.GetTopAppsAsync(today, today.AddDays(1));
                var usageDict = totals.ToDictionary(t => t.AppId, t => t.TotalSeconds);
                foreach (var lim in limits.Where(l => l.IsEnabled))
                {
                    if (usageDict.TryGetValue(lim.AppId, out var used) && used > lim.MaxDailySeconds)
                    {
                        limitsViolated = true;
                        break;
                    }
                }
            }
        }
        catch { }

        return (earliestStart, maxSessionSec, totalTodaySec, distinctApps, clicksToday, limitsViolated);
    }

    private static List<DailyQuest> GetQuestCatalog(bool isRu)
    {
        if (isRu)
        {
            return
            [
                new DailyQuest
                {
                    QuestType = "early_bird",
                    Title = "Ранняя пташка",
                    Description = "Начать работу за компьютером до 10:00 утра",
                    TargetValue = 1,
                    XpReward = 50,
                    Icon = "🌅"
                },
                new DailyQuest
                {
                    QuestType = "deep_focus",
                    Title = "Глубокий фокус",
                    Description = "Провести непрерывную сессию работы от 40 минут без пауз",
                    TargetValue = 2400,
                    XpReward = 80,
                    Icon = "🎯"
                },
                new DailyQuest
                {
                    QuestType = "screen_time",
                    Title = "Продуктивный день",
                    Description = "Накопить не менее 2 часов активности за день",
                    TargetValue = 7200,
                    XpReward = 70,
                    Icon = "⚡"
                },
                new DailyQuest
                {
                    QuestType = "variety",
                    Title = "Мультизадачность",
                    Description = "Поработать не менее чем в 3 различных программах",
                    TargetValue = 3,
                    XpReward = 60,
                    Icon = "🧩"
                },
                new DailyQuest
                {
                    QuestType = "mouse_clicks",
                    Title = "Мастер клика",
                    Description = "Сделать не менее 500 кликов мыши за день",
                    TargetValue = 500,
                    XpReward = 50,
                    Icon = "🖱️"
                },
                new DailyQuest
                {
                    QuestType = "discipline",
                    Title = "В рамках разумного",
                    Description = "Не превысить установленные лимиты времени приложений",
                    TargetValue = 1,
                    XpReward = 80,
                    Icon = "🛡️"
                }
            ];
        }

        return
        [
            new DailyQuest
            {
                QuestType = "early_bird",
                Title = "Early Bird",
                Description = "Start your PC activity before 10:00 AM",
                TargetValue = 1,
                XpReward = 50,
                Icon = "🌅"
            },
            new DailyQuest
            {
                QuestType = "deep_focus",
                Title = "Deep Focus",
                Description = "Complete a continuous focus session of 40+ minutes",
                TargetValue = 2400,
                XpReward = 80,
                Icon = "🎯"
            },
            new DailyQuest
            {
                QuestType = "screen_time",
                Title = "Productive Day",
                Description = "Accumulate at least 2 hours of active screen time",
                TargetValue = 7200,
                XpReward = 70,
                Icon = "⚡"
            },
            new DailyQuest
            {
                QuestType = "variety",
                Title = "Multitasker",
                Description = "Work with at least 3 different applications today",
                TargetValue = 3,
                XpReward = 60,
                Icon = "🧩"
            },
            new DailyQuest
            {
                QuestType = "mouse_clicks",
                Title = "Click Master",
                Description = "Achieve at least 500 mouse clicks today",
                TargetValue = 500,
                XpReward = 50,
                Icon = "🖱️"
            },
            new DailyQuest
            {
                QuestType = "discipline",
                Title = "Within Limits",
                Description = "Do not exceed any configured daily app limits",
                TargetValue = 1,
                XpReward = 80,
                Icon = "🛡️"
            }
        ];
    }
}
