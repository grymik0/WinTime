using Dapper;
using WinTime.Models;

namespace WinTime.Data;

/// <summary>
/// Repository for daily application limits and goals.
/// </summary>
public sealed class GoalRepository
{
    private readonly DatabaseService _database;

    public GoalRepository(DatabaseService database)
    {
        _database = database;
    }

    public async Task<List<AppLimitItem>> GetAllLimitsAsync()
    {
        var conn = _database.Connection;
        const string sql = @"
            SELECT 
                l.Id,
                l.AppId,
                a.ProcessName,
                a.DisplayName,
                a.IconBlob,
                l.MaxDailySeconds,
                l.ActionType,
                l.IsEnabled
            FROM AppLimits l
            JOIN Applications a ON l.AppId = a.Id
            ORDER BY a.DisplayName ASC, a.ProcessName ASC;
        ";

        var rows = await conn.QueryAsync<AppLimitItem>(sql);
        return rows.AsList();
    }

    public async Task<AppLimitItem?> GetLimitByAppIdAsync(int appId)
    {
        var conn = _database.Connection;
        const string sql = @"
            SELECT 
                l.Id,
                l.AppId,
                a.ProcessName,
                a.DisplayName,
                a.IconBlob,
                l.MaxDailySeconds,
                l.ActionType,
                l.IsEnabled
            FROM AppLimits l
            JOIN Applications a ON l.AppId = a.Id
            WHERE l.AppId = @AppId;
        ";

        return await conn.QueryFirstOrDefaultAsync<AppLimitItem>(sql, new { AppId = appId });
    }

    public async Task SetLimitAsync(int appId, int maxDailySeconds, LimitActionType actionType, bool isEnabled = true)
    {
        var conn = _database.Connection;
        const string sql = @"
            INSERT INTO AppLimits (AppId, MaxDailySeconds, ActionType, IsEnabled)
            VALUES (@AppId, @MaxDailySeconds, @ActionType, @IsEnabled)
            ON CONFLICT(AppId) DO UPDATE SET
                MaxDailySeconds = excluded.MaxDailySeconds,
                ActionType      = excluded.ActionType,
                IsEnabled       = excluded.IsEnabled;
        ";

        await conn.ExecuteAsync(sql, new
        {
            AppId = appId,
            MaxDailySeconds = maxDailySeconds,
            ActionType = (int)actionType,
            IsEnabled = isEnabled ? 1 : 0
        });
    }

    public async Task DeleteLimitAsync(int id)
    {
        var conn = _database.Connection;
        await conn.ExecuteAsync("DELETE FROM AppLimits WHERE Id = @Id;", new { Id = id });
    }

    public async Task ToggleLimitAsync(int id, bool isEnabled)
    {
        var conn = _database.Connection;
        await conn.ExecuteAsync("UPDATE AppLimits SET IsEnabled = @IsEnabled WHERE Id = @Id;", new
        {
            Id = id,
            IsEnabled = isEnabled ? 1 : 0
        });
    }
}
