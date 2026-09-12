using System.IO;

namespace WinTime.Models;

/// <summary>
/// Запись об отслеживаемом приложении (таблица Applications).
/// </summary>
public sealed class AppModel
{
    public int Id { get; set; }

    /// <summary>Имя исполняемого файла, например chrome.exe</summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>Читаемое имя, заданное пользователем (или авто из ProcessName).</summary>
    public string? DisplayName { get; set; }

    public string Category { get; set; } = "Без категории";

    /// <summary>Закэшированный Base64-путь к иконке (или null).</summary>
    public string? IconBlob { get; set; }

    /// <summary>Dapper читает INTEGER → bool через кастомный TypeHandler.</summary>
    public bool IsBlacklisted { get; set; }

    /// <summary>Отображаемое имя с fallback на имя процесса без расширения.</summary>
    public string FriendlyName =>
        !string.IsNullOrWhiteSpace(DisplayName)
            ? DisplayName
            : Path.GetFileNameWithoutExtension(ProcessName);
}
