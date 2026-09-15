using System.Collections.ObjectModel;
using System.Windows.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using WinTime.Core;
using WinTime.Data;
using WinTime.Models;
using WinTime.Services;

namespace WinTime.ViewModels;

public enum TimePeriod { Today, Week, Month }

/// <summary>
/// ViewModel главного дашборда:
/// — сводные карточки (экранное время / AFK / топ-приложение)
/// — Donut-диаграмма топ-5 приложений
/// — столбчатый график (часы/дни)
/// — список топ-приложений с ProgressBar
/// </summary>
public sealed class DashboardViewModel : BaseViewModel
{
    private readonly ActivityRepository _activityRepo;
    private readonly IconService        _iconService;

    // Bindable Properties

    private TimePeriod _selectedPeriod = TimePeriod.Today;
    private string _totalTime  = "—";
    private string _idleTime   = "—";
    private string _topApp     = "—";
    private bool   _isLoading;

    private ISeries[]  _pieSeries = [];
    private ISeries[]  _barSeries = [];
    private Axis[]     _xAxes    = [new Axis { Labels = [] }];
    private Axis[]     _yAxes    = [new Axis { MinLimit = 0 }];

    public SolidColorPaint LegendTextPaint { get; } =
        new(new SKColor(229, 231, 235));

    private ObservableCollection<AppStatItem> _topApps = [];

    public TimePeriod SelectedPeriod
    {
        get => _selectedPeriod;
        private set => SetProperty(ref _selectedPeriod, value);
    }

    public string TotalTime { get => _totalTime; private set => SetProperty(ref _totalTime, value); }
    public string IdleTime  { get => _idleTime;  private set => SetProperty(ref _idleTime,  value); }
    public string TopApp    { get => _topApp;    private set => SetProperty(ref _topApp,    value); }
    public bool   IsLoading { get => _isLoading; private set => SetProperty(ref _isLoading, value); }

    public ISeries[] PieSeries { get => _pieSeries; private set => SetProperty(ref _pieSeries, value); }
    public ISeries[] BarSeries { get => _barSeries; private set => SetProperty(ref _barSeries, value); }
    public Axis[]    XAxes    { get => _xAxes;    private set => SetProperty(ref _xAxes,    value); }
    public Axis[]    YAxes    { get => _yAxes;    private set => SetProperty(ref _yAxes,    value); }

    public ObservableCollection<AppStatItem> TopApps
    {
        get => _topApps;
        private set => SetProperty(ref _topApps, value);
    }

    public bool IsPeriodToday { get => _selectedPeriod == TimePeriod.Today;  }
    public bool IsPeriodWeek  { get => _selectedPeriod == TimePeriod.Week;   }
    public bool IsPeriodMonth { get => _selectedPeriod == TimePeriod.Month;  }

    private string _trendPercentageText = string.Empty;
    private string _trendDiffText       = string.Empty;
    private string _trendColorHex       = "#9CA3AF";
    private bool   _hasTrend;
    private string _idleRatioText       = string.Empty;
    private bool   _hasIdleRatio;
    private string _subMetricText       = string.Empty;
    private bool   _hasSubMetric;

    private string _trendPeriodLabel    = string.Empty;

    public string TrendPercentageText { get => _trendPercentageText; private set => SetProperty(ref _trendPercentageText, value); }
    public string TrendDiffText       { get => _trendDiffText;       private set => SetProperty(ref _trendDiffText,       value); }
    public string TrendPeriodLabel    { get => _trendPeriodLabel;    private set => SetProperty(ref _trendPeriodLabel,    value); }
    public string TrendColorHex       { get => _trendColorHex;       private set => SetProperty(ref _trendColorHex,       value); }
    public bool   HasTrend            { get => _hasTrend;            private set => SetProperty(ref _hasTrend,            value); }
    public string IdleRatioText       { get => _idleRatioText;       private set => SetProperty(ref _idleRatioText,       value); }
    public bool   HasIdleRatio        { get => _hasIdleRatio;        private set => SetProperty(ref _hasIdleRatio,        value); }
    public string SubMetricText       { get => _subMetricText;       private set => SetProperty(ref _subMetricText,       value); }
    public bool   HasSubMetric        { get => _hasSubMetric;        private set => SetProperty(ref _hasSubMetric,        value); }

    private ObservableCollection<HeatmapWeekItem> _heatmapWeeks = [];
    private string _heatmapStatsText = string.Empty;

    public ObservableCollection<HeatmapWeekItem> HeatmapWeeks
    {
        get => _heatmapWeeks;
        private set => SetProperty(ref _heatmapWeeks, value);
    }

    public string HeatmapStatsText
    {
        get => _heatmapStatsText;
        private set => SetProperty(ref _heatmapStatsText, value);
    }

    private string _heatmapTotalTimeText   = "0с";
    private string _heatmapBestDayText     = "—";
    private string _heatmapAvgDayText      = "—";
    private string _heatmapConsistencyText = "—";

    public string HeatmapTotalTimeText   { get => _heatmapTotalTimeText;   private set => SetProperty(ref _heatmapTotalTimeText,   value); }
    public string HeatmapBestDayText     { get => _heatmapBestDayText;     private set => SetProperty(ref _heatmapBestDayText,     value); }
    public string HeatmapAvgDayText      { get => _heatmapAvgDayText;      private set => SetProperty(ref _heatmapAvgDayText,      value); }
    public string HeatmapConsistencyText { get => _heatmapConsistencyText; private set => SetProperty(ref _heatmapConsistencyText, value); }

    private string _mouseClicksText   = "0";
    private string _mouseDistanceText = "0 м";
    public string MouseClicksText   { get => _mouseClicksText;   private set => SetProperty(ref _mouseClicksText,   value); }
    public string MouseDistanceText { get => _mouseDistanceText; private set => SetProperty(ref _mouseDistanceText, value); }

    // Commands

    public ICommand SetPeriodCommand { get; }

    // Colour palette

    private static readonly SKColor[] Palette =
    [
        SKColor.Parse("#6366F1"),
        SKColor.Parse("#8B5CF6"),
        SKColor.Parse("#EC4899"),
        SKColor.Parse("#F59E0B"),
        SKColor.Parse("#10B981"),
        SKColor.Parse("#6B7280"),
    ];

    private readonly ActivityTracker    _tracker;

    private long _rawActiveSeconds;
    private long _rawIdleSeconds;
    private long _rawPrevActiveSeconds;
    private DateTime _currentFrom = DateTime.Today;
    private int  _chartRefreshCounter;

    // Constructor

    public DashboardViewModel(ActivityRepository activityRepo, IconService iconService, ActivityTracker tracker)
    {
        _activityRepo = activityRepo;
        _iconService  = iconService;
        _tracker      = tracker;

        _tracker.StateChanged += OnTrackerStateChanged;

        SetPeriodCommand = new RelayCommand<TimePeriod>(p =>
        {
            SelectedPeriod = p;
            OnPropertyChanged(nameof(IsPeriodToday));
            OnPropertyChanged(nameof(IsPeriodWeek));
            OnPropertyChanged(nameof(IsPeriodMonth));
            _ = LoadDataAsync();
        });
    }

    private void OnTrackerStateChanged(object? sender, TrackerStateEventArgs e)
    {
        System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            if (!e.IsIdle)
            {
                _rawActiveSeconds++;
                TotalTime = Fmt(_rawActiveSeconds);

                var app = TopApps.FirstOrDefault(a => a.AppId == e.AppId);
                if (app is not null)
                {
                    app.AddSecond(_rawActiveSeconds);
                }
                else if (!string.IsNullOrEmpty(e.AppName))
                {
                    var newItem = new AppStatItem
                    {
                        AppId = e.AppId,
                        DisplayName = e.AppName,
                        ProcessName = e.AppName,
                        TotalSeconds = 1,
                        Percentage = _rawActiveSeconds > 0 ? 100.0 / _rawActiveSeconds : 0
                    };
                    TopApps.Add(newItem);
                }

                TopApp = TopApps.OrderByDescending(a => a.TotalSeconds).FirstOrDefault()?.DisplayName ?? "—";
            }
            else
            {
                _rawIdleSeconds++;
                IdleTime = Fmt(_rawIdleSeconds);
            }

            // Пересчитываем тренды и сравнение в реальном времени при каждом тике
            ComputeTrendsAndMetrics(_rawActiveSeconds, _rawIdleSeconds, _rawPrevActiveSeconds, _currentFrom);

            if (SelectedPeriod == TimePeriod.Today)
            {
                var (curClicks, curDistMeters) = _tracker.GetTodayMouseMetrics();
                MouseClicksText = FormatClicks(curClicks);
                MouseDistanceText = FormatDistance(curDistMeters);
            }

            if (++_chartRefreshCounter >= 30)
            {
                _chartRefreshCounter = 0;
                _ = RefreshChartsQuietlyAsync();
            }
        });
    }

    private async Task RefreshChartsQuietlyAsync()
    {
        try
        {
            var (from, _, barLabels) = GetPeriodRange();
            var apps = TopApps.ToList();
            long total = apps.Sum(a => a.TotalSeconds);
            BuildPieChart(apps, total);
            await BuildBarChartAsync(from, barLabels);
        }
        catch { }
    }

    // Data loading

    public async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            await _tracker.FlushToDbAsync();

            var (from, to, barLabels) = GetPeriodRange();
            _currentFrom = from;

            var (active, idle) = await _activityRepo.GetTotalsAsync(from, to);
            _rawActiveSeconds = active;
            _rawIdleSeconds   = idle;
            TotalTime = Fmt(active);
            IdleTime  = Fmt(idle);

            var (prevFrom, prevTo) = GetPreviousPeriodRange(from, to);
            var (prevActive, _)    = await _activityRepo.GetTotalsAsync(prevFrom, prevTo);
            _rawPrevActiveSeconds  = prevActive;

            ComputeTrendsAndMetrics(active, idle, prevActive, from);

            var apps = await _activityRepo.GetTopAppsAsync(from, to);
            TopApp = apps.FirstOrDefault()?.DisplayName ?? "—";

            long totalForPct = apps.Sum(a => a.TotalSeconds);
            foreach (var a in apps)
                a.Percentage = totalForPct > 0 ? a.TotalSeconds * 100.0 / totalForPct : 0;

            foreach (var a in apps.Take(10))
                a.Icon = null;

            TopApps = new ObservableCollection<AppStatItem>(apps.Take(10));

            BuildPieChart(apps, totalForPct);
            await BuildBarChartAsync(from, barLabels);
            await BuildHeatmapAsync();

            var (periodClicks, periodDistMeters) = await _activityRepo.GetDailyMetricsAsync(from, to);
            if (SelectedPeriod == TimePeriod.Today)
            {
                var (liveClicks, liveDist) = _tracker.GetTodayMouseMetrics();
                periodClicks = Math.Max(periodClicks, liveClicks);
                periodDistMeters = Math.Max(periodDistMeters, liveDist);
            }
            MouseClicksText = FormatClicks(periodClicks);
            MouseDistanceText = FormatDistance(periodDistMeters);
        }
        catch {  }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task BuildHeatmapAsync()
    {
        try
        {
            var today = DateTime.Today;
            int currentDayOfWeek = (int)today.DayOfWeek;
            int daysSinceMonday = currentDayOfWeek == 0 ? 6 : currentDayOfWeek - 1;
            DateTime currentWeekMonday = today.AddDays(-daysSinceMonday);
            DateTime startMonday = currentWeekMonday.AddDays(-19 * 7); // 20 недель

            var dailyMap = await _activityRepo.GetDailyActivityHistoryAsync(startMonday);
            var todayKey = today.ToString("yyyy-MM-dd");
            dailyMap[todayKey] = Math.Max(dailyMap.GetValueOrDefault(todayKey, 0L), _rawActiveSeconds);

            var weeks = new List<HeatmapWeekItem>();
            int previousMonth = -1;
            int activeDaysCount = 0;

            for (int w = 0; w < 20; w++)
            {
                var weekMonday = startMonday.AddDays(w * 7);
                var weekItem = new HeatmapWeekItem();

                if (weekMonday.Month != previousMonth)
                {
                    var mName = weekMonday.ToString("MMM", new System.Globalization.CultureInfo("ru-RU")).TrimEnd('.');
                    if (!string.IsNullOrEmpty(mName))
                        weekItem.MonthLabel = char.ToUpper(mName[0]) + mName[1..];
                    previousMonth = weekMonday.Month;
                }

                for (int d = 0; d < 7; d++)
                {
                    var dayDate = weekMonday.AddDays(d);
                    var dayKey = dayDate.ToString("yyyy-MM-dd");
                    bool isFuture = dayDate > today;
                    bool isToday = dayDate == today;
                    long sec = isFuture ? 0 : dailyMap.GetValueOrDefault(dayKey, 0L);

                    if (sec > 0) activeDaysCount++;

                    int intensity = 0;
                    string color = isFuture ? "#181824" : "#252535";

                    if (!isFuture && sec > 0)
                    {
                        if (sec < 3600)
                        {
                            intensity = 1;
                            color = "#3730A3";
                        }
                        else if (sec < 3 * 3600)
                        {
                            intensity = 2;
                            color = "#4F46E5";
                        }
                        else if (sec < 6 * 3600)
                        {
                            intensity = 3;
                            color = "#6366F1";
                        }
                        else
                        {
                            intensity = 4;
                            color = "#818CF8";
                        }
                    }

                    string dayFormatted = dayDate.ToString("d MMMM yyyy", new System.Globalization.CultureInfo("ru-RU"));
                    string tooltip = isFuture
                        ? dayFormatted
                        : (sec > 0 ? $"{dayFormatted}: {Fmt(sec)} активности" : $"{dayFormatted}: нет активности");

                    weekItem.Days.Add(new HeatmapDayItem
                    {
                        Date = dayDate,
                        ActiveSeconds = sec,
                        Intensity = intensity,
                        ColorHex = color,
                        TooltipText = tooltip,
                        IsFuture = isFuture,
                        IsToday = isToday
                    });
                }

                weeks.Add(weekItem);
            }

            int streak = 0;
            var checkDate = today;
            if (dailyMap.GetValueOrDefault(checkDate.ToString("yyyy-MM-dd"), 0L) == 0)
                checkDate = checkDate.AddDays(-1);

            while (dailyMap.GetValueOrDefault(checkDate.ToString("yyyy-MM-dd"), 0L) > 0)
            {
                streak++;
                checkDate = checkDate.AddDays(-1);
            }

            int totalDays = 20 * 7;
            long totalPeriodSec = 0;
            var bestDayDate = DateTime.MinValue;
            long bestDaySec = 0;

            foreach (var kv in dailyMap)
            {
                if (kv.Value > 0)
                {
                    totalPeriodSec += kv.Value;
                    if (kv.Value > bestDaySec && DateTime.TryParse(kv.Key, out var parsedDate))
                    {
                        bestDaySec = kv.Value;
                        bestDayDate = parsedDate;
                    }
                }
            }

            HeatmapTotalTimeText = Fmt(totalPeriodSec);
            HeatmapAvgDayText = activeDaysCount > 0 ? Fmt(totalPeriodSec / activeDaysCount) : "0м";
            HeatmapConsistencyText = $"{activeDaysCount} из {totalDays} дн. ({Math.Round((double)activeDaysCount / totalDays * 100):F0}%)";
            HeatmapBestDayText = bestDaySec > 0
                ? $"{bestDayDate.ToString("d MMM", new System.Globalization.CultureInfo("ru-RU"))} ({Fmt(bestDaySec)})"
                : "—";

            HeatmapStatsText = $"🔥 Серия: {streak} {GetDaysWord(streak)} · Всего активных: {activeDaysCount} дн.";
            HeatmapWeeks = new ObservableCollection<HeatmapWeekItem>(weeks);
        }
        catch { }
    }

    private static string GetDaysWord(int count)
    {
        int c10 = count % 10;
        int c100 = count % 100;
        if (c100 >= 11 && c100 <= 14) return "дней";
        if (c10 == 1) return "день";
        if (c10 >= 2 && c10 <= 4) return "дня";
        return "дней";
    }

    private (DateTime prevFrom, DateTime prevTo) GetPreviousPeriodRange(DateTime currentFrom, DateTime currentTo)
    {
        return _selectedPeriod switch
        {
            TimePeriod.Today => (currentFrom.AddDays(-1), currentFrom),
            TimePeriod.Week  => (currentFrom.AddDays(-7), currentFrom),
            TimePeriod.Month => (currentFrom.AddMonths(-1), currentFrom),
            _                => (currentFrom.AddDays(-1), currentFrom)
        };
    }

    private void ComputeTrendsAndMetrics(long active, long idle, long prevActive, DateTime from)
    {
        long totalSpan = active + idle;
        if (totalSpan > 0 && idle > 0)
        {
            double idlePct = (double)idle / totalSpan * 100.0;
            IdleRatioText = $"{idlePct:F0}% от общего времени";
            HasIdleRatio = true;
        }
        else
        {
            HasIdleRatio = false;
        }

        TrendPeriodLabel = SelectedPeriod switch
        {
            TimePeriod.Today => "по сравнению со вчера",
            TimePeriod.Week  => "по сравнению с прошлой неделей",
            _                => "по сравнению с прошлым месяцем"
        };

        if (prevActive == 0 && active == 0)
        {
            HasTrend = false;
        }
        else if (prevActive == 0)
        {
            TrendPercentageText = "+100%";
            TrendDiffText = $"на {Fmt(active)} больше";
            TrendColorHex = "#818CF8";
            HasTrend = true;
        }
        else
        {
            long diff = active - prevActive;
            double pct = (double)diff / prevActive * 100.0;
            string sign = pct > 0 ? "+" : "";
            TrendPercentageText = $"{sign}{pct:F0}%";

            if (diff < 0)
                TrendDiffText = $"на {Fmt(Math.Abs(diff))} меньше";
            else if (diff > 0)
                TrendDiffText = $"на {Fmt(diff)} больше";
            else
                TrendDiffText = "столько же";

            TrendColorHex = pct > 0 ? "#818CF8" : (pct < 0 ? "#34D399" : "#9CA3AF");
            HasTrend = true;
        }

        var now = DateTime.Now;
        if (SelectedPeriod == TimePeriod.Week)
        {
            int daysElapsed = (int)now.DayOfWeek == 0 ? 7 : (int)now.DayOfWeek;
            long avg = active / Math.Max(1, daysElapsed);
            SubMetricText = $"📊 В ср: {Fmt(avg)}/день ({daysElapsed} дн.)";
            HasSubMetric = true;
        }
        else if (SelectedPeriod == TimePeriod.Month)
        {
            int daysElapsed = Math.Max(1, now.Day);
            long avg = active / daysElapsed;
            SubMetricText = $"📊 В ср: {Fmt(avg)}/день ({daysElapsed} дн.)";
            HasSubMetric = true;
        }
        else
        {
            HasSubMetric = false;
        }
    }

    // Chart builders

    private void BuildPieChart(List<AppStatItem> apps, long total)
    {
        var top5  = apps.Take(5).ToList();
        long rest = apps.Skip(5).Sum(a => a.TotalSeconds);

        var series = new List<ISeries>();
        for (int i = 0; i < top5.Count; i++)
        {
            series.Add(new PieSeries<double>
            {
                Values      = [Math.Round(top5[i].TotalSeconds / 3600.0, 3)],
                Name        = top5[i].DisplayName,
                Fill        = new SolidColorPaint(Palette[i % Palette.Length]),
                Stroke      = null,
                InnerRadius = 55,
                MaxRadialColumnWidth = 30,
            });
        }

        if (rest > 0)
        {
            series.Add(new PieSeries<double>
            {
                Values      = [Math.Round(rest / 3600.0, 3)],
                Name        = "Остальные",
                Fill        = new SolidColorPaint(Palette[5]),
                Stroke      = null,
                InnerRadius = 55,
                MaxRadialColumnWidth = 30,
            });
        }

        PieSeries = [.. series];
    }

    private async Task BuildBarChartAsync(DateTime from, string[] labels)
    {
        long[] values = _selectedPeriod switch
        {
            TimePeriod.Today  => await _activityRepo.GetHourlyBreakdownAsync(from),
            TimePeriod.Week   => await _activityRepo.GetWeeklyBreakdownAsync(from),
            TimePeriod.Month  => await _activityRepo.GetMonthlyBreakdownAsync(from.Year, from.Month),
            _                 => []
        };

        var doubles = values.Select(v => Math.Round(v / 3600.0, 2)).ToArray();

        BarSeries =
        [
            new ColumnSeries<double>
            {
                Values               = doubles,
                Name                 = "Активное время (ч)",
                Fill                 = new SolidColorPaint(Palette[0]),
                Stroke               = null,
                MaxBarWidth          = double.MaxValue,
                IgnoresBarPosition   = true,
            }
        ];

        XAxes =
        [
            new Axis
            {
                Labels         = _selectedPeriod == TimePeriod.Week ? labels : null,
                Labeler        = _selectedPeriod switch
                {
                    TimePeriod.Today  => v => $"{(int)v:D2}:00",
                    TimePeriod.Month  => v => $"{(int)v + 1} чис.",
                    _                 => v => v.ToString(),
                },
                LabelsRotation = _selectedPeriod == TimePeriod.Today  ? -60 :
                                 _selectedPeriod == TimePeriod.Month  ? -60 : 0,
                TextSize       = 11,
                LabelsPaint    = new SolidColorPaint(new SKColor(156, 163, 175)),
                Padding        = new LiveChartsCore.Drawing.Padding(0),
            }
        ];

        YAxes =
        [
            new Axis
            {
                MinLimit    = 0,
                TextSize    = 11,
                LabelsPaint = new SolidColorPaint(new SKColor(156, 163, 175)),
                Labeler     = v => v < 1
                    ? $"{(int)Math.Round(v * 60)}м"
                    : $"{v:F1}ч",
            }
        ];
    }

    // Period range

    private (DateTime from, DateTime to, string[] labels) GetPeriodRange()
    {
        var now = DateTime.Now;
        return _selectedPeriod switch
        {
            TimePeriod.Today => (
                now.Date,
                now.Date.AddDays(1),
                Enumerable.Range(0, 24).Select(h => $"{h:D2}").ToArray()
            ),
            TimePeriod.Week => (
                now.Date.AddDays(-(int)now.DayOfWeek == 0 ? -6 : -(int)now.DayOfWeek + 1),
                now.Date.AddDays(7 - ((int)now.DayOfWeek == 0 ? 7 : (int)now.DayOfWeek) + 1),
                ["Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс"]
            ),
            TimePeriod.Month => (
                new DateTime(now.Year, now.Month, 1),
                new DateTime(now.Year, now.Month, 1).AddMonths(1),
                Enumerable.Range(1, DateTime.DaysInMonth(now.Year, now.Month))
                          .Select(d => d.ToString()).ToArray()
            ),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static string Fmt(long s)
    {
        var ts = TimeSpan.FromSeconds(s);
        if (ts.TotalHours >= 1)  return $"{(int)ts.TotalHours}ч {ts.Minutes:D2}м";
        if (ts.TotalMinutes >= 1) return $"{ts.Minutes}м {ts.Seconds:D2}с";
        return $"{ts.Seconds}с";
    }

    public static string FormatClicks(long clicks) => clicks switch
    {
        >= 1_000_000 => $"{clicks / 1_000_000.0:F1}M",
        >= 10_000    => $"{clicks / 1_000.0:F1}k",
        _            => $"{clicks:N0}"
    };

    public static string FormatDistance(double meters) => meters switch
    {
        >= 10_000 => $"{meters / 1000.0:F1} км",
        >= 1_000  => $"{meters / 1000.0:F2} км",
        _         => $"{meters:F0} м"
    };
}

