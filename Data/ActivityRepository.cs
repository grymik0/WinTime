using Dapper;
using WinTime.Models;

namespace WinTime.Data;

/// <summary>
/// CRUD и аналитические запросы для таблицы ActivitySessions.
/// </summary>
public sealed class ActivityRepository
{
    private readonly DatabaseService _db;

    public ActivityRepository(DatabaseService db) => _db = db;

    // ── Write ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Вставляет список сессий одной транзакцией (30-секундный flush).
    /// Сессии с DurationSeconds == 0 игнорируются.
    /// </summary>
    public async Task InsertBatchAsync(IReadOnlyList<ActivitySession> sessions)
    {
        var valid = sessions.Where(s => s.DurationSeconds > 0).ToList();
        if (valid.Count == 0) return;

        using var tx = _db.Connection.BeginTransaction();
        try
        {
            await _db.Connection.ExecuteAsync(@"
                INSERT INTO ActivitySessions (AppId, WindowTitle, StartTime, DurationSeconds, IsIdle)
                VALUES (@AppId, @WindowTitle, @StartTime, @DurationSeconds, @IsIdle)",
                valid.Select(s => new
                {
                    s.AppId,
                    s.WindowTitle,
                    StartTime = s.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    s.DurationSeconds,
                    IsIdle = s.IsIdle ? 1 : 0
                }),
                tx);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    // ── Analytics ─────────────────────────────────────────────────────────────

    /// <summary>Суммарное активное и idle время за период.</summary>
    public async Task<(long Active, long Idle)> GetTotalsAsync(DateTime from, DateTime to)
    {
        var row = await _db.Connection.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT
                SUM(CASE WHEN s.IsIdle = 0 THEN s.DurationSeconds ELSE 0 END) AS Active,
                SUM(CASE WHEN s.IsIdle = 1 THEN s.DurationSeconds ELSE 0 END) AS Idle
            FROM ActivitySessions s
            JOIN Applications a ON a.Id = s.AppId
            WHERE s.StartTime >= @From AND s.StartTime < @To
              AND a.IsBlacklisted = 0",
            new { From = Fmt(from), To = Fmt(to) });

        if (row is null) return (0, 0);
        return ((long)(row.Active ?? 0L), (long)(row.Idle ?? 0L));
    }

    /// <summary>Топ приложений по суммарному активному времени за период.</summary>
    public async Task<List<AppStatItem>> GetTopAppsAsync(DateTime from, DateTime to)
    {
        var rows = await _db.Connection.QueryAsync<dynamic>(@"
            SELECT
                a.Id AS AppId,
                a.ProcessName,
                COALESCE(a.DisplayName, a.ProcessName) AS DisplayName,
                a.IconBlob,
                SUM(s.DurationSeconds) AS TotalSeconds
            FROM ActivitySessions s
            JOIN Applications a ON a.Id = s.AppId
            WHERE s.StartTime >= @From AND s.StartTime < @To
              AND s.IsIdle = 0 AND a.IsBlacklisted = 0
            GROUP BY a.Id
            ORDER BY TotalSeconds DESC",
            new { From = Fmt(from), To = Fmt(to) });

        return rows.Select(r => new AppStatItem
        {
            AppId       = (int)r.AppId,
            ProcessName = (string)r.ProcessName,
            DisplayName = (string)r.DisplayName,
            TotalSeconds = (long)r.TotalSeconds
        }).ToList();
    }

    /// <summary>Почасовая разбивка за день (массив 24 значений, секунды).</summary>
    public async Task<long[]> GetHourlyBreakdownAsync(DateTime date)
    {
        var from = date.Date;
        var to   = from.AddDays(1);

        var rows = await _db.Connection.QueryAsync<dynamic>(@"
            SELECT CAST(strftime('%H', StartTime) AS INTEGER) AS Hour,
                   SUM(DurationSeconds) AS Total
            FROM ActivitySessions s
            JOIN Applications a ON a.Id = s.AppId
            WHERE s.StartTime >= @From AND s.StartTime < @To
              AND s.IsIdle = 0 AND a.IsBlacklisted = 0
            GROUP BY Hour",
            new { From = Fmt(from), To = Fmt(to) });

        var result = new long[24];
        foreach (var r in rows)
            result[(int)r.Hour] = (long)r.Total;
        return result;
    }

    /// <summary>Дневная разбивка за 7 дней недели (массив 7 значений, секунды).</summary>
    public async Task<long[]> GetWeeklyBreakdownAsync(DateTime weekStart)
    {
        var from = weekStart.Date;
        var to   = from.AddDays(7);

        var rows = await _db.Connection.QueryAsync<dynamic>(@"
            SELECT CAST(julianday(date(StartTime)) - julianday(@From) AS INTEGER) AS DayOffset,
                   SUM(DurationSeconds) AS Total
            FROM ActivitySessions s
            JOIN Applications a ON a.Id = s.AppId
            WHERE s.StartTime >= @From AND s.StartTime < @To
              AND s.IsIdle = 0 AND a.IsBlacklisted = 0
            GROUP BY DayOffset",
            new { From = Fmt(from), To = Fmt(to) });

        var result = new long[7];
        foreach (var r in rows)
        {
            int d = (int)r.DayOffset;
            if (d >= 0 && d < 7) result[d] = (long)r.Total;
        }
        return result;
    }

    /// <summary>Дневная разбивка за месяц (массив N значений, секунды).</summary>
    public async Task<long[]> GetMonthlyBreakdownAsync(int year, int month)
    {
        var from = new DateTime(year, month, 1);
        var to   = from.AddMonths(1);
        int days = DateTime.DaysInMonth(year, month);

        var rows = await _db.Connection.QueryAsync<dynamic>(@"
            SELECT CAST(strftime('%d', StartTime) AS INTEGER) AS Day,
                   SUM(DurationSeconds) AS Total
            FROM ActivitySessions s
            JOIN Applications a ON a.Id = s.AppId
            WHERE s.StartTime >= @From AND s.StartTime < @To
              AND s.IsIdle = 0 AND a.IsBlacklisted = 0
            GROUP BY Day",
            new { From = Fmt(from), To = Fmt(to) });

        var result = new long[days];
        foreach (var r in rows)
        {
            int d = (int)r.Day - 1;
            if (d >= 0 && d < days) result[d] = (long)r.Total;
        }
        return result;
    }

    public async Task ClearAllAsync()
    {
        await _db.Connection.ExecuteAsync("DELETE FROM ActivitySessions");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Fmt(DateTime dt) => dt.ToString("yyyy-MM-dd HH:mm:ss");
}

