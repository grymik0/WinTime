using System.Collections.ObjectModel;
using System.Windows.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
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

    // ── Bindable Properties ──────────────────────────────────────────────────

    private TimePeriod _selectedPeriod = TimePeriod.Today;
    private string _totalTime  = "—";
    private string _idleTime   = "—";
    private string _topApp     = "—";
    private bool   _isLoading;

    private ISeries[]  _pieSeries = [];
    private ISeries[]  _barSeries = [];
    private Axis[]     _xAxes    = [new Axis { Labels = [] }];
    private Axis[]     _yAxes    = [new Axis { MinLimit = 0 }];

    // Белый текст для легенды и осей на тёмном фоне
    public SolidColorPaint LegendTextPaint { get; } =
        new(new SKColor(229, 231, 235)); // #E5E7EB

    private static readonly SolidColorPaint AxisLabelPaint =
        new(new SKColor(156, 163, 175)); // #9CA3AF

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

    // Удобные флаги для подсветки кнопок периода
    public bool IsPeriodToday { get => _selectedPeriod == TimePeriod.Today;  }
    public bool IsPeriodWeek  { get => _selectedPeriod == TimePeriod.Week;   }
    public bool IsPeriodMonth { get => _selectedPeriod == TimePeriod.Month;  }

    // ── Commands ──────────────────────────────────────────────────────────────

    public ICommand SetPeriodCommand { get; }

    // ── Colour palette ────────────────────────────────────────────────────────

    private static readonly SKColor[] Palette =
    [
        SKColor.Parse("#6366F1"), // indigo
        SKColor.Parse("#8B5CF6"), // violet
        SKColor.Parse("#EC4899"), // pink
        SKColor.Parse("#F59E0B"), // amber
        SKColor.Parse("#10B981"), // emerald
        SKColor.Parse("#6B7280"), // gray — «Остальные»
    ];

    // ── Constructor ───────────────────────────────────────────────────────────

    public DashboardViewModel(ActivityRepository activityRepo, IconService iconService)
    {
        _activityRepo = activityRepo;
        _iconService  = iconService;

        SetPeriodCommand = new RelayCommand<TimePeriod>(p =>
        {
            SelectedPeriod = p;
            OnPropertyChanged(nameof(IsPeriodToday));
            OnPropertyChanged(nameof(IsPeriodWeek));
            OnPropertyChanged(nameof(IsPeriodMonth));
            _ = LoadDataAsync();
        });
    }

    // ── Data loading ──────────────────────────────────────────────────────────

    public async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var (from, to, barLabels) = GetPeriodRange();

            // Сводные цифры
            var (active, idle) = await _activityRepo.GetTotalsAsync(from, to);
            TotalTime = Fmt(active);
            IdleTime  = Fmt(idle);

            // Список приложений
            var apps = await _activityRepo.GetTopAppsAsync(from, to);
            TopApp = apps.FirstOrDefault()?.DisplayName ?? "—";

            long totalForPct = apps.Sum(a => a.TotalSeconds);
            foreach (var a in apps)
                a.Percentage = totalForPct > 0 ? a.TotalSeconds * 100.0 / totalForPct : 0;

            // Иконки (лениво, не блокируем UI)
            foreach (var a in apps.Take(10))
                a.Icon = null; // иконки будут добавлены позже через IconService + путь к EXE

            TopApps = new ObservableCollection<AppStatItem>(apps.Take(10));

            BuildPieChart(apps, totalForPct);
            await BuildBarChartAsync(from, barLabels);
        }
        catch { /* защита от любых ошибок БД */ }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Chart builders ────────────────────────────────────────────────────────

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
                Labels          = labels,
                LabelsRotation  = _selectedPeriod == TimePeriod.Month ? -60 : 0,
                TextSize        = 11,
                LabelsPaint     = AxisLabelPaint,
                Padding         = new LiveChartsCore.Drawing.Padding(0),
            }
        ];

        YAxes =
        [
            new Axis
            {
                MinLimit    = 0,
                TextSize    = 11,
                LabelsPaint = AxisLabelPaint,
                Labeler     = v => $"{v:F1}ч",
            }
        ];
    }

    // ── Period range ──────────────────────────────────────────────────────────

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
}

