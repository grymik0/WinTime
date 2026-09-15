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

    // Get or Create

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
        var category = IsKnownGame(processName) ? "Игры" : "Без категории";
        var id = await _db.Connection.ExecuteScalarAsync<int>(@"
            INSERT INTO Applications (ProcessName, DisplayName, Category)
            VALUES (@ProcessName, @DisplayName, @Category)
            ON CONFLICT(ProcessName) DO UPDATE SET ProcessName = excluded.ProcessName;
            SELECT Id FROM Applications WHERE ProcessName = @ProcessName;",
            new { ProcessName = processName, DisplayName = displayName, Category = category });

        var model = new AppModel
        {
            Id          = id,
            ProcessName = processName,
            DisplayName = displayName,
            Category    = category
        };
        _cache[processName] = model;
        return model;
    }

    private static readonly HashSet<string> KnownGameProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "csgo.exe", "cs2.exe", "dota2.exe", "dota.exe", "league of legends.exe", "valorant.exe", "valorant-win64-shipping.exe",
        "gta5.exe", "rdr2.exe", "cyberpunk2077.exe", "witcher3.exe", "minecraft.exe", "javaw.exe",
        "genshinimpact.exe", "starrail.exe", "zenlesszonezero.exe", "overwatch.exe", "apex.exe",
        "pubg.exe", "tslgame.exe", "fortniteclient-win64-shipping.exe", "rocketleague.exe",
        "rust.exe", "rustclient.exe", "worldoftanks.exe", "wot.exe", "war_thunder.exe", "aces.exe",
        "fifa.exe", "fc24.exe", "fc25.exe", "baldursgate3.exe", "bg3.exe", "bg3_dx11.exe",
        "eldenring.exe", "sekiro.exe", "dark souls.exe", "fallout4.exe", "skyrimse.exe", "skyrim.exe", "f1_25.exe", "f1_24.exe",
    };

    private static bool IsKnownGame(string processName)
    {
        var exe = Path.GetFileName(processName);
        return KnownGameProcesses.Contains(exe);
    }

    // Read

    public async Task<List<AppModel>> GetAllAsync()
    {
        var rows = await _db.Connection.QueryAsync<AppModel>(
            "SELECT * FROM Applications ORDER BY COALESCE(DisplayName, ProcessName)");
        return rows.AsList();
    }

    // Update

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

    // Helpers

    private void InvalidateCacheById(int id)
    {
        var key = _cache.FirstOrDefault(kv => kv.Value.Id == id).Key;
        if (key is not null)
            _cache.Remove(key);
    }
}
