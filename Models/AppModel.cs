using System.IO;

namespace WinTime.Models;

/// <summary>
/// Entity representing an application tracked in the Applications table.
/// </summary>
public sealed class AppModel
{
    public int Id { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string Category { get; set; } = "Без категории";
    public string? IconBlob { get; set; }
    public bool IsBlacklisted { get; set; }

    public string FriendlyName =>
        !string.IsNullOrWhiteSpace(DisplayName)
            ? DisplayName
            : Path.GetFileNameWithoutExtension(ProcessName);
}

