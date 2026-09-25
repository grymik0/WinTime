namespace WinTime.Models;

public sealed class UserStreakInfo
{
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }
    public string? LastActiveDate { get; set; }
    public long BonusXp { get; set; }
    public bool IsActiveToday { get; set; }
}

