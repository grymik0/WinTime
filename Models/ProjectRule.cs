namespace WinTime.Models;

/// <summary>
/// Rule mapping an application or window title keyword to a project.
/// </summary>
public sealed class ProjectRule
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int? AppId { get; set; }
    public string TitleKeyword { get; set; } = string.Empty;

    // Display fields populated via JOIN
    public string? ProcessName { get; set; }
    public string? AppDisplayName { get; set; }

    public string DisplayText
    {
        get
        {
            string appPart = !string.IsNullOrWhiteSpace(AppDisplayName)
                ? AppDisplayName
                : (!string.IsNullOrWhiteSpace(ProcessName) ? ProcessName : string.Empty);

            if (!string.IsNullOrWhiteSpace(appPart) && !string.IsNullOrWhiteSpace(TitleKeyword))
                return $"💻 {appPart} + 📄 \"{TitleKeyword}\"";

            if (!string.IsNullOrWhiteSpace(appPart))
                return $"💻 {appPart}";

            if (!string.IsNullOrWhiteSpace(TitleKeyword))
                return $"📄 Заголовок: \"{TitleKeyword}\"";

            return "Все активности";
        }
    }
}

