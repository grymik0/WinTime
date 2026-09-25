using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WinTime.Data;
using WinTime.Models;
using WinTime.Services;

namespace WinTime.ViewModels;

public enum ProjectPeriod
{
    Today,
    Week,
    Month,
    AllTime
}

public sealed class ProjectsViewModel : BaseViewModel
{
    private readonly ProjectRepository     _projectRepo;
    private readonly ApplicationRepository _appRepo;
    private readonly LocalizationService   _localization;

    private ProjectPeriod _selectedPeriod = ProjectPeriod.Week;
    private bool          _isLoading;

    private string _totalProjectTimeText = "0с";
    private string _topProjectName       = "—";
    private string _topProjectTimeText   = "0с";
    private string _coveragePercentText  = "0%";
    private string _projectsCountText    = "0 проектов";
    private string _unassignedTimeText   = "0с (100%)";

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public ProjectPeriod SelectedPeriod
    {
        get => _selectedPeriod;
        set
        {
            if (SetProperty(ref _selectedPeriod, value))
            {
                OnPropertyChanged(nameof(IsPeriodToday));
                OnPropertyChanged(nameof(IsPeriodWeek));
                OnPropertyChanged(nameof(IsPeriodMonth));
                OnPropertyChanged(nameof(IsPeriodAllTime));
                _ = LoadDataAsync();
            }
        }
    }

    public bool IsPeriodToday   => _selectedPeriod == ProjectPeriod.Today;
    public bool IsPeriodWeek    => _selectedPeriod == ProjectPeriod.Week;
    public bool IsPeriodMonth   => _selectedPeriod == ProjectPeriod.Month;
    public bool IsPeriodAllTime => _selectedPeriod == ProjectPeriod.AllTime;

    public string TotalProjectTimeText
    {
        get => _totalProjectTimeText;
        private set => SetProperty(ref _totalProjectTimeText, value);
    }

    public string TopProjectName
    {
        get => _topProjectName;
        private set => SetProperty(ref _topProjectName, value);
    }

    public string TopProjectTimeText
    {
        get => _topProjectTimeText;
        private set => SetProperty(ref _topProjectTimeText, value);
    }

    public string CoveragePercentText
    {
        get => _coveragePercentText;
        private set => SetProperty(ref _coveragePercentText, value);
    }

    public string ProjectsCountText
    {
        get => _projectsCountText;
        private set => SetProperty(ref _projectsCountText, value);
    }

    public string UnassignedTimeText
    {
        get => _unassignedTimeText;
        private set => SetProperty(ref _unassignedTimeText, value);
    }

    public ObservableCollection<ProjectStatItem> Projects { get; } = [];

    public ICommand SetPeriodCommand        { get; }
    public ICommand CreateProjectCommand    { get; }
    public ICommand EditProjectCommand      { get; }
    public ICommand DeleteProjectCommand    { get; }
    public ICommand AddRuleCommand          { get; }
    public ICommand DeleteRuleCommand       { get; }
    public ICommand ReapplyAllRulesCommand  { get; }

    public ProjectsViewModel(
        ProjectRepository projectRepo,
        ApplicationRepository appRepo,
        LocalizationService localization)
    {
        _projectRepo  = projectRepo;
        _appRepo      = appRepo;
        _localization = localization;

        SetPeriodCommand = new RelayCommand<ProjectPeriod>(p => SelectedPeriod = p);
        CreateProjectCommand = new RelayCommand(async () => await CreateProjectAsync());
        EditProjectCommand = new RelayCommand<ProjectStatItem>(async p => await EditProjectAsync(p));
        DeleteProjectCommand = new RelayCommand<ProjectStatItem>(async p => await DeleteProjectAsync(p));
        AddRuleCommand = new RelayCommand<ProjectStatItem>(async p => await AddRuleAsync(p));
        DeleteRuleCommand = new RelayCommand<ProjectRule>(async r => await DeleteRuleAsync(r));
        ReapplyAllRulesCommand = new RelayCommand(async () => await ReapplyRulesAsync());
    }

    public async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var (from, to) = GetPeriodRange();
            var (stats, unassigned, totalActive) = await _projectRepo.GetProjectStatsAsync(from, to);

            long totalProjectSec = stats.Sum(s => s.TotalSeconds);
            TotalProjectTimeText = LocalizationService.FormatDuration(totalProjectSec);

            var top = stats.FirstOrDefault(s => s.TotalSeconds > 0);
            if (top != null)
            {
                TopProjectName = $"{top.Icon} {top.Name}";
                TopProjectTimeText = top.FormattedTime;
            }
            else
            {
                TopProjectName = "—";
                TopProjectTimeText = "0с";
            }

            double coverage = totalActive > 0 ? (double)totalProjectSec / totalActive * 100.0 : 0.0;
            CoveragePercentText = $"{coverage:F0}%";

            int totalRules = stats.Sum(s => s.RulesCount);
            ProjectsCountText = $"{stats.Count} проектов · {totalRules} правил";

            double unassignedPct = totalActive > 0 ? (double)unassigned / totalActive * 100.0 : 0.0;
            UnassignedTimeText = $"{LocalizationService.FormatDuration(unassigned)} ({unassignedPct:F0}%)";

            Projects.Clear();
            foreach (var item in stats)
            {
                Projects.Add(item);
            }
        }
        catch { }
        finally
        {
            IsLoading = false;
        }
    }

    private (DateTime from, DateTime to) GetPeriodRange()
    {
        var now = DateTime.Now;
        var today = DateTime.Today;

        return _selectedPeriod switch
        {
            ProjectPeriod.Today => (today, today.AddDays(1)),
            ProjectPeriod.Week => (
                today.AddDays(-(int)today.DayOfWeek == 0 ? -6 : -(int)today.DayOfWeek + 1),
                today.AddDays(1)
            ),
            ProjectPeriod.Month => (
                new DateTime(now.Year, now.Month, 1),
                today.AddDays(1)
            ),
            ProjectPeriod.AllTime => (DateTime.MinValue, today.AddDays(1)),
            _ => (today, today.AddDays(1))
        };
    }

    private async Task CreateProjectAsync()
    {
        var dlg = new Views.ProjectEditDialog
        {
            Owner = Application.Current?.MainWindow
        };

        if (dlg.ShowDialog() == true)
        {
            var project = new Project
            {
                Name = dlg.ProjectName,
                Description = dlg.ProjectDesc,
                ColorHex = dlg.SelectedColor,
                Icon = dlg.SelectedIcon,
                CreatedAt = DateTime.UtcNow
            };

            await _projectRepo.CreateAsync(project);
            await LoadDataAsync();
        }
    }

    private async Task EditProjectAsync(ProjectStatItem? item)
    {
        if (item is null) return;

        var existing = await _projectRepo.GetByIdAsync(item.Id);
        if (existing is null) return;

        var dlg = new Views.ProjectEditDialog(existing)
        {
            Owner = Application.Current?.MainWindow
        };

        if (dlg.ShowDialog() == true)
        {
            existing.Name = dlg.ProjectName;
            existing.Description = dlg.ProjectDesc;
            existing.ColorHex = dlg.SelectedColor;
            existing.Icon = dlg.SelectedIcon;

            await _projectRepo.UpdateAsync(existing);
            await LoadDataAsync();
        }
    }

    private async Task DeleteProjectAsync(ProjectStatItem? item)
    {
        if (item is null) return;

        var result = MessageBox.Show(
            $"Удалить проект «{item.Icon} {item.Name}»?\nВсе привязанные правила будут удалены, а время вернется в нераспределенное.",
            "WinTime",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            await _projectRepo.DeleteAsync(item.Id);
            await LoadDataAsync();
        }
    }

    private async Task AddRuleAsync(ProjectStatItem? item)
    {
        if (item is null) return;

        var apps = await _appRepo.GetAllAsync();
        var dlg = new Views.ProjectRuleDialog(apps)
        {
            Owner = Application.Current?.MainWindow
        };

        if (dlg.ShowDialog() == true)
        {
            var rule = new ProjectRule
            {
                ProjectId = item.Id,
                AppId = dlg.SelectedAppId,
                TitleKeyword = dlg.Keyword
            };

            await _projectRepo.AddRuleAsync(rule);
            await LoadDataAsync();
        }
    }

    private async Task DeleteRuleAsync(ProjectRule? rule)
    {
        if (rule is null) return;

        var result = MessageBox.Show(
            $"Удалить правило «{rule.DisplayText}»?",
            "WinTime",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            await _projectRepo.DeleteRuleAsync(rule.Id);
            await LoadDataAsync();
        }
    }

    private async Task ReapplyRulesAsync()
    {
        IsLoading = true;
        try
        {
            await _projectRepo.ApplyRulesToSessionsAsync();
            await LoadDataAsync();
            MessageBox.Show("Правила успешно применены ко всем прошлым записям активности!", "WinTime", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch { }
        finally
        {
            IsLoading = false;
        }
    }
}

