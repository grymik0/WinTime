namespace WinTime.Models;

/// <summary>
/// Запись одной сессии активности (таблица ActivitySessions).
/// </summary>
public sealed class ActivitySession
{
    public int Id { get; set; }

    public int AppId { get; set; }

    /// <summary>Заголовок окна во время сессии.</summary>
    public string WindowTitle { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    /// <summary>Длительность сессии в секундах.</summary>
    public int DurationSeconds { get; set; }

    /// <summary>true — сессия засчитана в AFK / бездействие.</summary>
    public bool IsIdle { get; set; }
}

