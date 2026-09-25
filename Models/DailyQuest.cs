using WinTime.ViewModels;

namespace WinTime.Models;

public sealed class DailyQuest : BaseViewModel
{
    private int _currentValue;
    private bool _isCompleted;
    private bool _isClaimed;
    private string _progressText = string.Empty;

    public int Id { get; set; }
    public string Date { get; set; } = string.Empty; // "yyyy-MM-dd"
    public string QuestType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TargetValue { get; set; }
    public int XpReward { get; set; } = 50;
    public string Icon { get; set; } = "🎯";

    public int CurrentValue
    {
        get => _currentValue;
        set
        {
            if (SetProperty(ref _currentValue, value))
            {
                OnPropertyChanged(nameof(ProgressPercentage));
            }
        }
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set
        {
            if (SetProperty(ref _isCompleted, value))
            {
                OnPropertyChanged(nameof(CanClaim));
                OnPropertyChanged(nameof(StatusBadgeText));
            }
        }
    }

    public bool IsClaimed
    {
        get => _isClaimed;
        set
        {
            if (SetProperty(ref _isClaimed, value))
            {
                OnPropertyChanged(nameof(CanClaim));
                OnPropertyChanged(nameof(StatusBadgeText));
            }
        }
    }

    public bool CanClaim => IsCompleted && !IsClaimed;

    public double ProgressPercentage => TargetValue <= 0 ? (IsCompleted ? 100 : 0) : Math.Clamp((double)CurrentValue / TargetValue * 100.0, 0, 100);

    public string ProgressText
    {
        get => _progressText;
        set => SetProperty(ref _progressText, value);
    }

    public string StatusBadgeText
    {
        get
        {
            if (IsClaimed) return Services.LocalizationService.IsRussian ? "Получено ✓" : "Claimed ✓";
            if (IsCompleted) return Services.LocalizationService.IsRussian ? "Выполнено!" : "Completed!";
            return Services.LocalizationService.IsRussian ? "В процессе" : "In Progress";
        }
    }
}

