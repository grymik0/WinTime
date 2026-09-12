using System.IO;
using Dapper;
using WinTime.Models;

namespace WinTime.Data;

/// <summary>
/// CRUD-операции для таблицы Applications.
/// Содержит кэш в памяти для часто запрашиваемых приложений.
/// </summary>
public sealed class ApplicationRepository
{
    private readonly DatabaseService _db;

    // in-memory кэш: ProcessName → AppModel (для горячего пути трекера)
    private readonly Dictionary<string, AppModel> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    public ApplicationRepository(DatabaseService db) => _db = db;

    // ── Get or Create ────────────────────────────────────────────────────────

    /// <summary>
    /// Ищет приложение по ProcessName в кэше и БД.
    /// Создаёт новую запись, если приложение встречается впервые.
    /// </summary>
    public async Task<AppModel> GetOrCreateAsync(string processName, string processPath)
    {
        if (_cache.TryGetValue(processName, out var cached))
            return cached;

        var existing = await _db.Connection.QueryFirstOrDefaultAsync<AppModel>(
            "SELECT * FROM Applications WHERE ProcessName = @ProcessName COLLATE NOCASE",
            new { ProcessName = processName });

        if (existing is not null)
        {
            _cache[processName] = existing;
            return existing;
        }

        // Первый раз — создаём запись, DisplayName = имя без расширения
        var displayName = Path.GetFileNameWithoutExtension(processName);
        var id = await _db.Connection.ExecuteScalarAsync<int>(@"
            INSERT INTO Applications (ProcessName, DisplayName)
            VALUES (@ProcessName, @DisplayName)
            ON CONFLICT(ProcessName) DO UPDATE SET ProcessName = excluded.ProcessName;
            SELECT Id FROM Applications WHERE ProcessName = @ProcessName;",
            new { ProcessName = processName, DisplayName = displayName });

        var model = new AppModel
        {
            Id          = id,
            ProcessName = processName,
            DisplayName = displayName
        };
        _cache[processName] = model;
        return model;
    }

    // ── Read ─────────────────────────────────────────────────────────────────

    public async Task<List<AppModel>> GetAllAsync()
    {
        var rows = await _db.Connection.QueryAsync<AppModel>(
            "SELECT * FROM Applications ORDER BY COALESCE(DisplayName, ProcessName)");
        return rows.AsList();
    }

    // ── Update ───────────────────────────────────────────────────────────────

    public async Task UpdateDisplayNameAsync(int id, string displayName)
    {
        await _db.Connection.ExecuteAsync(
            "UPDATE Applications SET DisplayName = @DisplayName WHERE Id = @Id",
            new { Id = id, DisplayName = displayName });
        InvalidateCacheById(id);
    }

    public async Task UpdateCategoryAsync(int id, string category)
    {
        await _db.Connection.ExecuteAsync(
            "UPDATE Applications SET Category = @Category WHERE Id = @Id",
            new { Id = id, Category = category });
        InvalidateCacheById(id);
    }

    public async Task SetBlacklistAsync(int id, bool isBlacklisted)
    {
        await _db.Connection.ExecuteAsync(
            "UPDATE Applications SET IsBlacklisted = @IsBlacklisted WHERE Id = @Id",
            new { Id = id, IsBlacklisted = isBlacklisted });
        InvalidateCacheById(id);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void InvalidateCacheById(int id)
    {
        var key = _cache.FirstOrDefault(kv => kv.Value.Id == id).Key;
        if (key is not null)
            _cache.Remove(key);
    }
}
