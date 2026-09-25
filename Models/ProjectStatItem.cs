using WinTime.Services;
using WinTime.ViewModels;

namespace WinTime.Models;

/// <summary>
/// Display model for a project in the projects dashboard.
/// </summary>
public sealed class ProjectStatItem : BaseViewModel
{
    private long _totalSeconds;
    private double _percentage;

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#6366F1";
    public string Icon { get; set; } = "📁";
    public string Description { get; set; } = string.Empty;

    public long TotalSeconds
    {
        get => _totalSeconds;
        set
        {
            if (SetProperty(ref _totalSeconds, value))
                OnPropertyChanged(nameof(FormattedTime));
        }
    }

    public double Percentage
    {
        get => _percentage;
        set => SetProperty(ref _percentage, value);
    }

    public string FormattedTime => LocalizationService.FormatDuration(TotalSeconds);

    public List<ProjectRule> Rules { get; set; } = [];
    public int RulesCount => Rules.Count;

    public string RulesSummaryText => RulesCount switch
    {
        0 => "Нет правил привязки",
        1 => Rules[0].DisplayText,
        2 => $"{Rules[0].DisplayText}, {Rules[1].DisplayText}",
        _ => $"{Rules[0].DisplayText} и ещё {RulesCount - 1}"
    };
}

