using System.Collections.ObjectModel;
using System.Windows.Input;
using WinTime.Core;
using WinTime.Data;
using WinTime.Models;

namespace WinTime.ViewModels;

public sealed class ProcessesViewModel : BaseViewModel
{
    private readonly UptimeRepository _uptimeRepo;
    private readonly ActivityTracker  _tracker;

    private List<ProcessUptimeItem> _allItems = [];
    private ObservableCollection<ProcessUptimeItem> _items = [];
    private TimePeriod _selectedPeriod = TimePeriod.Today;
    private string _searchText = string.Empty;
    private bool _isLoading;

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

    public ICommand SetTodayCommand   { get; }
    public ICommand SetWeekCommand    { get; }
    public ICommand SetMonthCommand   { get; }
    public ICommand RefreshCommand    { get; }

    public ProcessesViewModel(UptimeRepository uptimeRepo, ActivityTracker tracker)
    {
        _uptimeRepo = uptimeRepo;
        _tracker    = tracker;

        _tracker.StateChanged += OnTrackerStateChanged;

        SetTodayCommand = new RelayCommand(() => SelectedPeriod = TimePeriod.Today);
        SetWeekCommand  = new RelayCommand(() => SelectedPeriod = TimePeriod.Week);
        SetMonthCommand = new RelayCommand(() => SelectedPeriod = TimePeriod.Month);
        RefreshCommand  = new RelayCommand(async () => await LoadAsync());
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
