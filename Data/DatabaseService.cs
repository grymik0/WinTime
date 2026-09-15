using System.Data;
using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;

namespace WinTime.Data;

/// <summary>
/// Инициализирует SQLite-соединение и создаёт схему БД при первом запуске.
/// Держит единственное открытое соединение на весь жизненный цикл приложения.
/// </summary>
public sealed class DatabaseService : IDisposable
{
    private SqliteConnection? _connection;

    public SqliteConnection Connection =>
        _connection ?? throw new InvalidOperationException("DatabaseService не инициализирован.");

    // Initialization

    public void Initialize(string dbPath)
    {
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();

        // Настраиваем SQLite для надёжной и быстрой работы
        ApplyPragmas();

        // Регистрируем TypeHandler для bool ↔ INTEGER
        SqlMapper.AddTypeHandler(new BoolTypeHandler());

        // Создаём таблицы если их нет
        CreateSchema();
    }

    private void ApplyPragmas()
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            PRAGMA journal_mode = WAL;
            PRAGMA synchronous   = NORMAL;
            PRAGMA foreign_keys  = ON;
            PRAGMA cache_size    = -8192;
        ";
        cmd.ExecuteNonQuery();
    }

    private void CreateSchema()
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Applications (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                ProcessName   TEXT    NOT NULL UNIQUE,
                DisplayName   TEXT,
                Category      TEXT    NOT NULL DEFAULT 'Без категории',
                IconBlob      TEXT,
                IsBlacklisted INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS ActivitySessions (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                AppId           INTEGER NOT NULL REFERENCES Applications(Id),
                WindowTitle     TEXT    NOT NULL DEFAULT '',
                StartTime       TEXT    NOT NULL,
                DurationSeconds INTEGER NOT NULL DEFAULT 0,
                IsIdle          INTEGER NOT NULL DEFAULT 0
            );

            CREATE INDEX IF NOT EXISTS idx_sessions_start ON ActivitySessions(StartTime);
            CREATE INDEX IF NOT EXISTS idx_sessions_appid ON ActivitySessions(AppId);
            CREATE INDEX IF NOT EXISTS idx_sessions_appid_start ON ActivitySessions(AppId, StartTime);

            CREATE TABLE IF NOT EXISTS AppUptime (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                AppId         INTEGER NOT NULL REFERENCES Applications(Id),
                Date          TEXT    NOT NULL,
                UptimeSeconds INTEGER NOT NULL DEFAULT 0,
                UNIQUE(AppId, Date)
            );

            CREATE INDEX IF NOT EXISTS idx_app_uptime_date ON AppUptime(Date);

            CREATE TABLE IF NOT EXISTS DailyMetrics (
                Date            TEXT PRIMARY KEY,
                MouseClicks     INTEGER NOT NULL DEFAULT 0,
                DistanceMeters  REAL    NOT NULL DEFAULT 0
            );
        ";
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _connection = null;
    }

    // Dapper TypeHandler: INTEGER ↔ bool

    private sealed class BoolTypeHandler : SqlMapper.TypeHandler<bool>
    {
        public override bool Parse(object value) => Convert.ToBoolean(value);

        public override void SetValue(IDbDataParameter parameter, bool value)
            => parameter.Value = value ? 1 : 0;
    }
}
