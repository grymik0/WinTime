using Dapper;
using WinTime.Models;

namespace WinTime.Data;

/// <summary>
/// Repository for managing Projects, ProjectRules, and project-based activity statistics.
/// </summary>
public sealed class ProjectRepository
{
    private readonly DatabaseService _db;
    private readonly object _cacheLock = new();
    private List<ProjectRule> _cachedRules = [];

    public ProjectRepository(DatabaseService db)
    {
        _db = db;
        _ = RefreshRulesCacheAsync();
    }

    public async Task RefreshRulesCacheAsync()
    {
        try
        {
            var rules = await GetAllRulesAsync();
            lock (_cacheLock)
            {
                _cachedRules = rules;
            }
        }
        catch { }
    }

    public int? MatchProjectId(int appId, string windowTitle)
    {
        lock (_cacheLock)
        {
            string lowerTitle = windowTitle.ToLowerInvariant();
            foreach (var rule in _cachedRules)
            {
                bool appMatch = !rule.AppId.HasValue || rule.AppId.Value == appId;
                bool titleMatch = string.IsNullOrWhiteSpace(rule.TitleKeyword) || lowerTitle.Contains(rule.TitleKeyword.ToLowerInvariant());

                if (rule.AppId.HasValue && !string.IsNullOrWhiteSpace(rule.TitleKeyword))
                {
                    if (appMatch && titleMatch) return rule.ProjectId;
                }
                else if (rule.AppId.HasValue)
                {
                    if (appMatch) return rule.ProjectId;
                }
                else if (!string.IsNullOrWhiteSpace(rule.TitleKeyword))
                {
                    if (titleMatch) return rule.ProjectId;
                }
            }
            return null;
        }
    }

    public async Task<List<Project>> GetAllAsync()
    {
        var rows = await _db.Connection.QueryAsync<Project>(@"
            SELECT Id, Name, ColorHex, Icon, Description, CreatedAt
            FROM Projects
            ORDER BY Name ASC");
        return rows.ToList();
    }

    public async Task<Project?> GetByIdAsync(int id)
    {
        return await _db.Connection.QueryFirstOrDefaultAsync<Project>(@"
            SELECT Id, Name, ColorHex, Icon, Description, CreatedAt
            FROM Projects
            WHERE Id = @Id", new { Id = id });
    }

    public async Task<int> CreateAsync(Project project)
    {
        var id = await _db.Connection.ExecuteScalarAsync<int>(@"
            INSERT INTO Projects (Name, ColorHex, Icon, Description, CreatedAt)
            VALUES (@Name, @ColorHex, @Icon, @Description, @CreatedAt);
            SELECT last_insert_rowid();",
            new
            {
                project.Name,
                project.ColorHex,
                project.Icon,
                project.Description,
                CreatedAt = project.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            });

        project.Id = id;
        return id;
    }

    public async Task UpdateAsync(Project project)
    {
        await _db.Connection.ExecuteAsync(@"
            UPDATE Projects
            SET Name = @Name,
                ColorHex = @ColorHex,
                Icon = @Icon,
                Description = @Description
            WHERE Id = @Id", project);
    }

    public async Task DeleteAsync(int id)
    {
        using var tx = _db.Connection.BeginTransaction();
        try
        {
            await _db.Connection.ExecuteAsync(
                "UPDATE ActivitySessions SET ProjectId = NULL WHERE ProjectId = @Id",
                new { Id = id }, tx);

            await _db.Connection.ExecuteAsync(
                "DELETE FROM ProjectRules WHERE ProjectId = @Id",
                new { Id = id }, tx);

            await _db.Connection.ExecuteAsync(
                "DELETE FROM Projects WHERE Id = @Id",
                new { Id = id }, tx);

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }

        await RefreshRulesCacheAsync();
    }

    public async Task<List<ProjectRule>> GetAllRulesAsync()
    {
        var rows = await _db.Connection.QueryAsync<ProjectRule>(@"
            SELECT r.Id, r.ProjectId, r.AppId, r.TitleKeyword,
                   a.ProcessName, a.DisplayName AS AppDisplayName
            FROM ProjectRules r
            LEFT JOIN Applications a ON a.Id = r.AppId
            ORDER BY r.ProjectId ASC, r.Id ASC");
        return rows.ToList();
    }

    public async Task<List<ProjectRule>> GetRulesForProjectAsync(int projectId)
    {
        var rows = await _db.Connection.QueryAsync<ProjectRule>(@"
            SELECT r.Id, r.ProjectId, r.AppId, r.TitleKeyword,
                   a.ProcessName, a.DisplayName AS AppDisplayName
            FROM ProjectRules r
            LEFT JOIN Applications a ON a.Id = r.AppId
            WHERE r.ProjectId = @ProjectId
            ORDER BY r.Id ASC", new { ProjectId = projectId });
        return rows.ToList();
    }

    public async Task<int> AddRuleAsync(ProjectRule rule)
    {
        var id = await _db.Connection.ExecuteScalarAsync<int>(@"
            INSERT INTO ProjectRules (ProjectId, AppId, TitleKeyword)
            VALUES (@ProjectId, @AppId, @TitleKeyword);
            SELECT last_insert_rowid();",
            new
            {
                rule.ProjectId,
                rule.AppId,
                TitleKeyword = rule.TitleKeyword.Trim()
            });

        rule.Id = id;
        await RefreshRulesCacheAsync();
        await ApplyRulesToSessionsAsync();
        return id;
    }

    public async Task DeleteRuleAsync(int ruleId)
    {
        await _db.Connection.ExecuteAsync(
            "DELETE FROM ProjectRules WHERE Id = @Id", new { Id = ruleId });

        await RefreshRulesCacheAsync();
        await ApplyRulesToSessionsAsync();
    }

    /// <summary>
    /// Re-evaluates all rules across the entire ActivitySessions table.
    /// </summary>
    public async Task ApplyRulesToSessionsAsync()
    {
        var rules = await GetAllRulesAsync();

        using var tx = _db.Connection.BeginTransaction();
        try
        {
            await _db.Connection.ExecuteAsync("UPDATE ActivitySessions SET ProjectId = NULL", transaction: tx);

            foreach (var r in rules)
            {
                string kw = r.TitleKeyword?.Trim() ?? string.Empty;
                if (r.AppId.HasValue && !string.IsNullOrWhiteSpace(kw))
                {
                    await _db.Connection.ExecuteAsync(@"
                        UPDATE ActivitySessions
                        SET ProjectId = @ProjectId
                        WHERE AppId = @AppId AND LOWER(WindowTitle) LIKE '%' || LOWER(@Keyword) || '%'",
                        new { r.ProjectId, AppId = r.AppId.Value, Keyword = kw }, tx);
                }
                else if (r.AppId.HasValue)
                {
                    await _db.Connection.ExecuteAsync(@"
                        UPDATE ActivitySessions
                        SET ProjectId = @ProjectId
                        WHERE AppId = @AppId",
                        new { r.ProjectId, AppId = r.AppId.Value }, tx);
                }
                else if (!string.IsNullOrWhiteSpace(kw))
                {
                    await _db.Connection.ExecuteAsync(@"
                        UPDATE ActivitySessions
                        SET ProjectId = @ProjectId
                        WHERE LOWER(WindowTitle) LIKE '%' || LOWER(@Keyword) || '%'",
                        new { r.ProjectId, Keyword = kw }, tx);
                }
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Gets aggregated active time per project for a given date range.
    /// </summary>
    public async Task<(List<ProjectStatItem> Projects, long UnassignedSeconds, long TotalActiveSeconds)> GetProjectStatsAsync(DateTime from, DateTime to)
    {
        var fromStr = from.ToString("yyyy-MM-dd HH:mm:ss");
        var toStr = to.ToString("yyyy-MM-dd HH:mm:ss");

        var projects = await GetAllAsync();
        var rules = await GetAllRulesAsync();
        var rulesByProj = rules.GroupBy(r => r.ProjectId).ToDictionary(g => g.Key, g => g.ToList());

        var timeRows = await _db.Connection.QueryAsync<dynamic>(@"
            SELECT s.ProjectId, SUM(s.DurationSeconds) AS TotalSec
            FROM ActivitySessions s
            JOIN Applications a ON a.Id = s.AppId
            WHERE s.StartTime >= @From AND s.StartTime < @To
              AND s.IsIdle = 0 AND a.IsBlacklisted = 0
            GROUP BY s.ProjectId",
            new { From = fromStr, To = toStr });

        var timeDict = new Dictionary<int, long>();
        long unassigned = 0;
        long totalActive = 0;

        foreach (var r in timeRows)
        {
            long sec = r.TotalSec != null ? (long)r.TotalSec : 0L;
            totalActive += sec;
            if (r.ProjectId != null)
            {
                timeDict[(int)r.ProjectId] = sec;
            }
            else
            {
                unassigned += sec;
            }
        }

        var statItems = new List<ProjectStatItem>();
        foreach (var p in projects)
        {
            long pSec = timeDict.GetValueOrDefault(p.Id, 0L);
            double pct = totalActive > 0 ? (double)pSec / totalActive * 100.0 : 0.0;

            statItems.Add(new ProjectStatItem
            {
                Id = p.Id,
                Name = p.Name,
                ColorHex = p.ColorHex,
                Icon = p.Icon,
                Description = p.Description,
                TotalSeconds = pSec,
                Percentage = pct,
                Rules = rulesByProj.GetValueOrDefault(p.Id, [])
            });
        }

        statItems = statItems.OrderByDescending(s => s.TotalSeconds).ThenBy(s => s.Name).ToList();

        return (statItems, unassigned, totalActive);
    }
}

