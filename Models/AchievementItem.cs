namespace WinTime.Models;

public sealed class AchievementItem
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Icon { get; init; } = "🏆";
    public bool IsUnlocked { get; set; }
    public double Progress { get; set; } // 0 - 100
    public string ProgressText { get; set; } = string.Empty;
    public string Category { get; init; } = "Время";

    public string BackgroundBrush => IsUnlocked ? "#1E293B" : "#181825";
    public string BorderBrush     => IsUnlocked ? "#6366F1" : "#2A2A3C";
    public string IconOpacity     => IsUnlocked ? "1.0" : "0.35";
    public string TitleForeground => IsUnlocked ? "White" : "#9CA3AF";
    public string StatusBadgeText { get; set; } = string.Empty;
    public string StatusBadgeBg   => IsUnlocked ? "#065F46" : "#252535";
    public string StatusBadgeFg   => IsUnlocked ? "#34D399" : "#6B7280";
}
