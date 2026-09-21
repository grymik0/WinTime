using Dapper;
using WinTime.Models;

namespace WinTime.Data;

/// <summary>
/// Repository for tracking and aggregating background application uptime.
/// </summary>
public sealed class UptimeRepository
{
    private readonly DatabaseService _db;

    public UptimeRepository(DatabaseService db)
    {
        _db = db;
    }

    /// <summary>
    /// Atomically increments uptime seconds for processes on the specified date.
    /// </summary>
    public async Task AddUptimeBatchAsync(string date, IEnumerable<(int AppId, int Seconds)> updates)
    {
        const string sql = @"
            INSERT INTO AppUptime (AppId, Date, UptimeSeconds)
            VALUES (@AppId, @Date, @Seconds)
            ON CONFLICT(AppId, Date) DO UPDATE
            SET UptimeSeconds = UptimeSeconds + @Seconds;
        ";

        var list = updates.Select(u => new { u.AppId, Date = date, u.Seconds }).ToList();
        if (list.Count == 0) return;

        using var tx = _db.Connection.BeginTransaction();
        await _db.Connection.ExecuteAsync(sql, list, tx);
        tx.Commit();
    }

    /// <summary>
    /// Returns aggregated uptime and focused active metrics across a date range.
    /// </summary>
    public async Task<List<ProcessUptimeItem>> GetUptimeStatsAsync(DateTime from, DateTime to)
    {
        var fromStr = from.ToString("yyyy-MM-dd");
        var toStr   = to.ToString("yyyy-MM-dd");

        const string sql = @"
            WITH UptimeAgg AS (
                SELECT AppId, SUM(UptimeSeconds) AS TotalUptime
                FROM AppUptime
                WHERE Date >= @fromStr AND Date <= @toStr
                GROUP BY AppId
            ),
            ActiveAgg AS (
                SELECT AppId, SUM(DurationSeconds) AS TotalActive
                FROM ActivitySessions
                WHERE StartTime >= @fromStr AND StartTime <= @toStr || ' 23:59:59'
                  AND IsIdle = 0
                GROUP BY AppId
            )
            SELECT 
                a.Id AS AppId,
                a.ProcessName,
                a.DisplayName,
                a.Category,
                a.IconBlob,
                a.IsBlacklisted,
                COALESCE(u.TotalUptime, 0) AS UptimeSeconds,
                COALESCE(act.TotalActive, 0) AS ActiveSeconds
            FROM Applications a
            LEFT JOIN UptimeAgg u ON a.Id = u.AppId
            LEFT JOIN ActiveAgg act ON a.Id = act.AppId
            WHERE (u.TotalUptime > 0 OR act.TotalActive > 0)
              AND a.IsBlacklisted = 0
            ORDER BY u.TotalUptime DESC;
        ";

        var rows = await _db.Connection.QueryAsync<ProcessUptimeItem>(sql, new { fromStr, toStr });
        return rows.ToList();
    }
}

