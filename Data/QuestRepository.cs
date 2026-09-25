using Dapper;
using WinTime.Models;

namespace WinTime.Data;

public sealed class QuestRepository
{
    private readonly DatabaseService _db;

    public QuestRepository(DatabaseService db)
    {
        _db = db;
    }

    public async Task<List<DailyQuest>> GetQuestsForDateAsync(string date)
    {
        var rows = await _db.Connection.QueryAsync<DailyQuest>(@"
            SELECT
                Id,
                Date,
                QuestType,
                Title,
                Description,
                TargetValue,
                CurrentValue,
                IsCompleted,
                IsClaimed,
                XpReward,
                Icon
            FROM DailyQuests
            WHERE Date = @Date
            ORDER BY Id ASC",
            new { Date = date });

        return rows.ToList();
    }

    public async Task InsertQuestsAsync(IEnumerable<DailyQuest> quests)
    {
        foreach (var q in quests)
        {
            await _db.Connection.ExecuteAsync(@"
                INSERT OR IGNORE INTO DailyQuests 
                    (Date, QuestType, Title, Description, TargetValue, CurrentValue, IsCompleted, IsClaimed, XpReward, Icon)
                VALUES 
                    (@Date, @QuestType, @Title, @Description, @TargetValue, @CurrentValue, @IsCompleted, @IsClaimed, @XpReward, @Icon)",
                q);
        }
    }

    public async Task UpdateQuestProgressAsync(int questId, int currentValue, bool isCompleted)
    {
        await _db.Connection.ExecuteAsync(@"
            UPDATE DailyQuests
            SET CurrentValue = @CurrentValue,
                IsCompleted = CASE WHEN IsCompleted = 1 THEN 1 ELSE @IsCompleted END
            WHERE Id = @Id",
            new { Id = questId, CurrentValue = currentValue, IsCompleted = isCompleted ? 1 : 0 });
    }

    public async Task<bool> ClaimQuestRewardAsync(int questId, int xpReward)
    {
        var updated = await _db.Connection.ExecuteAsync(@"
            UPDATE DailyQuests
            SET IsClaimed = 1
            WHERE Id = @Id AND IsCompleted = 1 AND IsClaimed = 0",
            new { Id = questId });

        if (updated > 0)
        {
            await _db.Connection.ExecuteAsync(@"
                UPDATE UserStreaks
                SET BonusXp = BonusXp + @XpReward
                WHERE Id = 1",
                new { XpReward = xpReward });
            return true;
        }

        return false;
    }

    public async Task<UserStreakInfo> GetUserStreakAsync()
    {
        var row = await _db.Connection.QuerySingleOrDefaultAsync<dynamic>(@"
            SELECT CurrentStreak, BestStreak, LastActiveDate, BonusXp
            FROM UserStreaks
            WHERE Id = 1");

        if (row == null)
        {
            return new UserStreakInfo();
        }

        return new UserStreakInfo
        {
            CurrentStreak  = (int)(row.CurrentStreak ?? 0),
            BestStreak     = (int)(row.BestStreak ?? 0),
            LastActiveDate = (string?)row.LastActiveDate,
            BonusXp        = (long)(row.BonusXp ?? 0)
        };
    }

    public async Task UpdateStreakAsync(int currentStreak, int bestStreak, string? lastActiveDate)
    {
        await _db.Connection.ExecuteAsync(@"
            UPDATE UserStreaks
            SET CurrentStreak = @CurrentStreak,
                BestStreak = @BestStreak,
                LastActiveDate = @LastActiveDate
            WHERE Id = 1",
            new { CurrentStreak = currentStreak, BestStreak = bestStreak, LastActiveDate = lastActiveDate });
    }

    public async Task<long> GetBonusXpAsync()
    {
        return await _db.Connection.ExecuteScalarAsync<long>(@"
            SELECT COALESCE(BonusXp, 0)
            FROM UserStreaks
            WHERE Id = 1");
    }
}

