namespace WinTime.Models;

/// <summary>
/// Entity representing a user-defined project or tag.
/// </summary>
public sealed class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#6366F1";
    public string Icon { get; set; } = "📁";
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

