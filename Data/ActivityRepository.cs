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

    // Write

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

    // Analytics

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

    /// <summary>Получает статистику активности по играм за период.</summary>
    public async Task<List<AppStatItem>> GetGamesActivityAsync(DateTime from, DateTime to)
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
              AND a.Category = 'Игры'
            GROUP BY a.Id
            ORDER BY TotalSeconds DESC",
            new { From = Fmt(from), To = Fmt(to) });

        return rows.Select(r => new AppStatItem
        {
            AppId        = (int)r.AppId,
            ProcessName  = (string)r.ProcessName,
            DisplayName  = (string)r.DisplayName,
            TotalSeconds = (long)r.TotalSeconds
        }).ToList();
    }

    /// <summary>Топ заголовков окон/вкладок для конкретного приложения за период.</summary>
    public async Task<List<(string Title, long Seconds)>> GetWindowTitlesForAppAsync(int appId, DateTime from, DateTime to, int limit = 50)
    {
        var rows = await _db.Connection.QueryAsync<dynamic>(@"
            SELECT
                s.WindowTitle,
                SUM(s.DurationSeconds) AS TotalSeconds
            FROM ActivitySessions s
            WHERE s.AppId = @AppId
              AND s.StartTime >= @From AND s.StartTime < @To
              AND s.IsIdle = 0
              AND TRIM(s.WindowTitle) != ''
            GROUP BY s.WindowTitle
            ORDER BY TotalSeconds DESC
            LIMIT @Limit",
            new { AppId = appId, From = Fmt(from), To = Fmt(to), Limit = limit });

        return rows.Select(r => ((string)r.WindowTitle, (long)r.TotalSeconds)).ToList();
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

    /// <summary>Суммарное активное время по дням начиная с fromDate (словарь yyyy-MM-dd -> секунды).</summary>
    public async Task<Dictionary<string, long>> GetDailyActivityHistoryAsync(DateTime fromDate)
    {
        var rows = await _db.Connection.QueryAsync<dynamic>(@"
            SELECT date(s.StartTime) AS DayDate,
                   SUM(s.DurationSeconds) AS Total
            FROM ActivitySessions s
            JOIN Applications a ON a.Id = s.AppId
            WHERE s.StartTime >= @FromDate
              AND s.IsIdle = 0
              AND a.IsBlacklisted = 0
            GROUP BY DayDate",
            new { FromDate = Fmt(fromDate) });

        var dict = new Dictionary<string, long>();
        foreach (var r in rows)
        {
            if (r.DayDate is not null && r.Total is not null)
                dict[(string)r.DayDate] = (long)r.Total;
        }
        return dict;
    }

    public async Task SaveDailyMetricsAsync(string date, long clicks, double distanceMeters)
    {
        await _db.Connection.ExecuteAsync(@"
            INSERT INTO DailyMetrics (Date, MouseClicks, DistanceMeters)
            VALUES (@Date, @MouseClicks, @DistanceMeters)
            ON CONFLICT(Date) DO UPDATE SET
                MouseClicks = MouseClicks + @MouseClicks,
                DistanceMeters = DistanceMeters + @DistanceMeters",
            new { Date = date, MouseClicks = clicks, DistanceMeters = distanceMeters });
    }

    public async Task<(long Clicks, double DistanceMeters)> GetDailyMetricsAsync(DateTime from, DateTime to)
    {
        var row = await _db.Connection.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT SUM(MouseClicks) AS Clicks, SUM(DistanceMeters) AS Distance
            FROM DailyMetrics
            WHERE Date >= @FromDate AND Date < @ToDate",
            new { FromDate = from.ToString("yyyy-MM-dd"), ToDate = to.ToString("yyyy-MM-dd") });

        long clicks = row?.Clicks != null ? (long)row.Clicks : 0L;
        double dist = row?.Distance != null ? (double)row.Distance : 0.0;
        return (clicks, dist);
    }

    public async Task<(long LifetimeActiveSeconds, long MaxDaySeconds, int TotalActiveDays, long TotalClicks, double TotalDistanceMeters)> GetLifetimeStatsAsync()
    {
        var row = await _db.Connection.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT 
                SUM(s.DurationSeconds) AS TotalActive
            FROM ActivitySessions s
            JOIN Applications a ON a.Id = s.AppId
            WHERE s.IsIdle = 0 AND a.IsBlacklisted = 0");

        var dayRows = await _db.Connection.QueryAsync<dynamic>(@"
            SELECT date(s.StartTime) AS DayDate, SUM(s.DurationSeconds) AS DayTotal
            FROM ActivitySessions s
            JOIN Applications a ON a.Id = s.AppId
            WHERE s.IsIdle = 0 AND a.IsBlacklisted = 0
            GROUP BY DayDate");

        var mouseRow = await _db.Connection.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT SUM(MouseClicks) AS Clicks, SUM(DistanceMeters) AS Dist FROM DailyMetrics");

        long lifetimeActive = row?.TotalActive != null ? (long)row.TotalActive : 0L;
        long maxDay = 0;
        int activeDays = 0;

        foreach (var dr in dayRows)
        {
            activeDays++;
            long dt = dr.DayTotal != null ? (long)dr.DayTotal : 0L;
            if (dt > maxDay) maxDay = dt;
        }

        long totalClicks = mouseRow?.Clicks != null ? (long)mouseRow.Clicks : 0L;
        double totalDist = mouseRow?.Dist != null ? (double)mouseRow.Dist : 0.0;

        return (lifetimeActive, maxDay, activeDays, totalClicks, totalDist);
    }

    // Helpers

    private static string Fmt(DateTime dt) => dt.ToString("yyyy-MM-dd HH:mm:ss");
}

