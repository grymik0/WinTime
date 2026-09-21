namespace WinTime.Models;

/// <summary>
/// Entity representing an activity session recorded in the database.
/// </summary>
public sealed class ActivitySession
{
    public int Id { get; set; }
    public int AppId { get; set; }
    public string WindowTitle { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public int DurationSeconds { get; set; }
    public bool IsIdle { get; set; }
}

