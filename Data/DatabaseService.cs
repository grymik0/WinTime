using System.Data;
using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;

namespace WinTime.Data;

/// <summary>
/// Initializes SQLite database connection and schema.
/// Maintains a single open connection for the application lifecycle.
/// </summary>
public sealed class DatabaseService : IDisposable
{
    private SqliteConnection? _connection;

    public SqliteConnection Connection =>
        _connection ?? throw new InvalidOperationException("DatabaseService is not initialized.");

    public void Initialize(string dbPath)
    {
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();

        ApplyPragmas();
        SqlMapper.AddTypeHandler(new BoolTypeHandler());
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

            CREATE TABLE IF NOT EXISTS AppLimits (
                Id               INTEGER PRIMARY KEY AUTOINCREMENT,
                AppId            INTEGER NOT NULL REFERENCES Applications(Id) ON DELETE CASCADE,
                MaxDailySeconds  INTEGER NOT NULL,
                ActionType       INTEGER NOT NULL DEFAULT 0,
                IsEnabled        INTEGER NOT NULL DEFAULT 1,
                UNIQUE(AppId)
            );

            CREATE TABLE IF NOT EXISTS Projects (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Name        TEXT    NOT NULL,
                ColorHex    TEXT    NOT NULL DEFAULT '#6366F1',
                Icon        TEXT    NOT NULL DEFAULT '📁',
                Description TEXT    NOT NULL DEFAULT '',
                CreatedAt   TEXT    NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ProjectRules (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                ProjectId     INTEGER NOT NULL REFERENCES Projects(Id) ON DELETE CASCADE,
                AppId         INTEGER NULL REFERENCES Applications(Id) ON DELETE CASCADE,
                TitleKeyword  TEXT    NOT NULL DEFAULT ''
            );

            CREATE TABLE IF NOT EXISTS UserStreaks (
                Id             INTEGER PRIMARY KEY CHECK (Id = 1),
                CurrentStreak  INTEGER NOT NULL DEFAULT 0,
                BestStreak     INTEGER NOT NULL DEFAULT 0,
                LastActiveDate TEXT    NULL,
                BonusXp        INTEGER NOT NULL DEFAULT 0
            );
            INSERT OR IGNORE INTO UserStreaks (Id, CurrentStreak, BestStreak, LastActiveDate, BonusXp)
            VALUES (1, 0, 0, NULL, 0);

            CREATE TABLE IF NOT EXISTS DailyQuests (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                Date         TEXT    NOT NULL,
                QuestType    TEXT    NOT NULL,
                Title        TEXT    NOT NULL,
                Description  TEXT    NOT NULL,
                TargetValue  INTEGER NOT NULL,
                CurrentValue INTEGER NOT NULL DEFAULT 0,
                IsCompleted  INTEGER NOT NULL DEFAULT 0,
                IsClaimed    INTEGER NOT NULL DEFAULT 0,
                XpReward     INTEGER NOT NULL DEFAULT 50,
                Icon         TEXT    NOT NULL DEFAULT '🎯',
                UNIQUE(Date, QuestType)
            );
            CREATE INDEX IF NOT EXISTS idx_daily_quests_date ON DailyQuests(Date);
        ";
        cmd.ExecuteNonQuery();

        EnsureColumnExists("ActivitySessions", "ProjectId", "INTEGER NULL REFERENCES Projects(Id)");
        using var idxCmd = _connection!.CreateCommand();
        idxCmd.CommandText = "CREATE INDEX IF NOT EXISTS idx_sessions_project ON ActivitySessions(ProjectId);";
        idxCmd.ExecuteNonQuery();
    }

    private void EnsureColumnExists(string table, string column, string typeDefinition)
    {
        using var checkCmd = _connection!.CreateCommand();
        checkCmd.CommandText = $"PRAGMA table_info({table});";
        using var reader = checkCmd.ExecuteReader();
        bool exists = false;
        while (reader.Read())
        {
            if (string.Equals(reader["name"]?.ToString(), column, StringComparison.OrdinalIgnoreCase))
            {
                exists = true;
                break;
            }
        }
        reader.Close();

        if (!exists)
        {
            using var alterCmd = _connection!.CreateCommand();
            alterCmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {typeDefinition};";
            alterCmd.ExecuteNonQuery();
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _connection = null;
    }

    private sealed class BoolTypeHandler : SqlMapper.TypeHandler<bool>
    {
        public override bool Parse(object value) => Convert.ToBoolean(value);

        public override void SetValue(IDbDataParameter parameter, bool value)
            => parameter.Value = value ? 1 : 0;
    }
}

