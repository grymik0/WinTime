using System.Collections.ObjectModel;
using System.Windows.Input;
using WinTime.Core;
using WinTime.Data;
using WinTime.Models;

namespace WinTime.ViewModels;

public sealed class ProcessesViewModel : BaseViewModel
{
    private readonly UptimeRepository  _uptimeRepo;
    private readonly ActivityRepository _activityRepo;
    private readonly ActivityTracker   _tracker;

    private List<ProcessUptimeItem> _allItems = [];
    private ObservableCollection<ProcessUptimeItem> _items = [];
    private TimePeriod _selectedPeriod = TimePeriod.Today;
    private string _searchText = string.Empty;
    private bool _isLoading;

    private static readonly string[] TitleSuffixes =
    [
        " - Google Chrome",
        " - Microsoft Edge",
        " - Brave",
        " - Mozilla Firefox",
        " - Opera",
        " - Visual Studio Code",
        " - Visual Studio",
        " - Telegram",
        " - Discord"
    ];

    public ObservableCollection<ProcessUptimeItem> Items
    {
        get => _items;
        private set => SetProperty(ref _items, value);
    }

    public string SearchText
    {
        get => _searchText;
        set { SetProperty(ref _searchText, value); ApplyFilter(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public TimePeriod SelectedPeriod
    {
        get => _selectedPeriod;
        set
        {
            if (SetProperty(ref _selectedPeriod, value))
            {
                OnPropertyChanged(nameof(IsPeriodToday));
                OnPropertyChanged(nameof(IsPeriodWeek));
                OnPropertyChanged(nameof(IsPeriodMonth));
                _ = LoadAsync();
            }
        }
    }

    public bool IsPeriodToday => _selectedPeriod == TimePeriod.Today;
    public bool IsPeriodWeek  => _selectedPeriod == TimePeriod.Week;
    public bool IsPeriodMonth => _selectedPeriod == TimePeriod.Month;

    public ICommand SetTodayCommand     { get; }
    public ICommand SetWeekCommand      { get; }
    public ICommand SetMonthCommand     { get; }
    public ICommand RefreshCommand      { get; }
    public ICommand ToggleExpandCommand { get; }

    public ProcessesViewModel(UptimeRepository uptimeRepo, ActivityRepository activityRepo, ActivityTracker tracker)
    {
        _uptimeRepo   = uptimeRepo;
        _activityRepo = activityRepo;
        _tracker      = tracker;

        _tracker.StateChanged += OnTrackerStateChanged;

        SetTodayCommand     = new RelayCommand(() => SelectedPeriod = TimePeriod.Today);
        SetWeekCommand      = new RelayCommand(() => SelectedPeriod = TimePeriod.Week);
        SetMonthCommand     = new RelayCommand(() => SelectedPeriod = TimePeriod.Month);
        RefreshCommand      = new RelayCommand(async () => await LoadAsync());
        ToggleExpandCommand = new RelayCommand<ProcessUptimeItem>(async item =>
        {
            if (item is null) return;
            await ToggleExpandAsync(item);
        });
    }

    private void OnTrackerStateChanged(object? sender, TrackerStateEventArgs e)
    {
        System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            var runningIds = _tracker.GetRunningAppIds();

            foreach (var item in _allItems)
            {
                bool isRunning = runningIds.Contains(item.AppId);
                item.IsCurrentlyRunning = isRunning;

                if (isRunning)
                {
                    item.TickUptime();
                }

                if (item.AppId == e.AppId && !e.IsIdle)
                {
                    item.TickActive();

                    if (item.IsExpanded && !string.IsNullOrWhiteSpace(e.WindowTitle))
                    {
                        var titleItem = item.WindowTitles.FirstOrDefault(t => t.RawTitle == e.WindowTitle);
                        if (titleItem is not null)
                        {
                            titleItem.TotalSeconds++;
                        }
                        else
                        {
                            var newItem = new WindowTitleStatItem
                            {
                                RawTitle = e.WindowTitle,
                                DisplayTitle = CleanWindowTitle(e.WindowTitle, item.ProcessName),
                                TotalSeconds = 1
                            };
                            item.WindowTitles.Add(newItem);
                        }

                        long grand = item.WindowTitles.Sum(t => t.TotalSeconds);
                        if (grand > 0)
                        {
                            foreach (var t in item.WindowTitles)
                                t.Percentage = Math.Round((double)t.TotalSeconds / grand * 100.0, 1);
                        }
                    }
                }
            }

            if (e.AppId > 0 && !_allItems.Any(a => a.AppId == e.AppId))
            {
                var newItem = new ProcessUptimeItem
                {
                    AppId = e.AppId,
                    DisplayName = e.AppName,
                    ProcessName = e.AppName,
                    UptimeSeconds = 1,
                    ActiveSeconds = e.IsIdle ? 0 : 1,
                    IsCurrentlyRunning = true
                };
                _allItems.Add(newItem);
                ApplyFilter();
            }
        });
    }

    private async Task ToggleExpandAsync(ProcessUptimeItem item)
    {
        item.IsExpanded = !item.IsExpanded;
        if (item.IsExpanded)
        {
            await LoadTitlesForItemAsync(item);
        }
    }

    public async Task LoadTitlesForItemAsync(ProcessUptimeItem item)
    {
        item.IsLoadingTitles = true;
        try
        {
            await _tracker.FlushToDbAsync();

            var (from, to) = GetPeriodRange();
            var toEnd = to.AddDays(1);
            var titles = await _activityRepo.GetWindowTitlesForAppAsync(item.AppId, from, toEnd, 40);
            long grandTotal = titles.Sum(t => t.Seconds);

            var list = titles.Select(t => new WindowTitleStatItem
            {
                RawTitle     = t.Title,
                DisplayTitle = CleanWindowTitle(t.Title, item.ProcessName),
                TotalSeconds = t.Seconds,
                Percentage   = grandTotal > 0 ? Math.Round((double)t.Seconds / grandTotal * 100.0, 1) : 0
            }).ToList();

            item.WindowTitles = new ObservableCollection<WindowTitleStatItem>(list);
        }
        catch { }
        finally
        {
            item.IsLoadingTitles = false;
        }
    }

    public static string CleanWindowTitle(string title, string processName)
    {
        if (string.IsNullOrWhiteSpace(title)) return "—";
        var cleaned = title.Trim();
        foreach (var suffix in TitleSuffixes)
        {
            if (cleaned.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned[..^suffix.Length].Trim();
                break;
            }
        }
        return string.IsNullOrWhiteSpace(cleaned) ? title.Trim() : cleaned;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await _tracker.FlushToDbAsync();

            var (from, to) = GetPeriodRange();
            var list = await _uptimeRepo.GetUptimeStatsAsync(from, to);

            var runningIds = _tracker.GetRunningAppIds();
            foreach (var item in list)
            {
                item.IsCurrentlyRunning = runningIds.Contains(item.AppId);
            }

            // Сохраняем состояние раскрытия
            var expandedAppIds = _allItems.Where(i => i.IsExpanded).Select(i => i.AppId).ToHashSet();
            foreach (var item in list)
            {
                if (expandedAppIds.Contains(item.AppId))
                {
                    item.IsExpanded = true;
                    _ = LoadTitlesForItemAsync(item);
                }
            }

            _allItems = list;
            ApplyFilter();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter()
    {
        var q = _searchText.Trim();
        var filtered = string.IsNullOrEmpty(q)
            ? _allItems
            : _allItems.Where(a =>
                a.FriendlyName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                a.ProcessName.Contains(q, StringComparison.OrdinalIgnoreCase));

        Items = new ObservableCollection<ProcessUptimeItem>(filtered);
    }

    private (DateTime from, DateTime to) GetPeriodRange()
    {
        var now = DateTime.Now;
        return _selectedPeriod switch
        {
            TimePeriod.Today => (now.Date, now.Date),
            TimePeriod.Week  => (now.Date.AddDays(-(int)now.DayOfWeek + (now.DayOfWeek == DayOfWeek.Sunday ? -6 : 1)), now.Date),
            TimePeriod.Month => (new DateTime(now.Year, now.Month, 1), now.Date),
            _                => (now.Date, now.Date)
        };
    }
}
