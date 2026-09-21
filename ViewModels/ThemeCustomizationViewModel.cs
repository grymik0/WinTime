using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using WinTime.Services;

namespace WinTime.ViewModels;

public sealed class ThemeItem
{
    public AppThemeMode Mode { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "";
    public Brush CardBg { get; set; } = Brushes.Transparent;
    public Brush CardBorder { get; set; } = Brushes.Transparent;
    public Brush TextColor { get; set; } = Brushes.White;
}

public sealed class AccentItem
{
    public AccentColorOption Option { get; set; }
    public string Name { get; set; } = "";
    public string HexColor { get; set; } = "";
    public Brush ColorBrush { get; set; } = Brushes.Transparent;
}

public sealed class ThemeCustomizationViewModel : BaseViewModel
{
    private readonly ThemeService _themeService;
    private readonly LocalizationService _localization;

    public ObservableCollection<ThemeItem> Themes { get; } = new();
    public ObservableCollection<AccentItem> Accents { get; } = new();

    public AppThemeMode CurrentTheme => _themeService.CurrentTheme;
    public AccentColorOption CurrentAccent => _themeService.CurrentAccent;
    public AppLanguage CurrentLanguage => _localization.CurrentLanguage;

    public bool IsRussian => CurrentLanguage == AppLanguage.Ru;
    public bool IsEnglish => CurrentLanguage == AppLanguage.En;

    public bool IsDarkTheme => CurrentTheme == AppThemeMode.Dark;
    public bool IsLightTheme => CurrentTheme == AppThemeMode.Light;
    public bool IsMidnightTheme => CurrentTheme == AppThemeMode.Midnight;

    public ICommand SelectThemeCommand { get; }
    public ICommand SelectAccentCommand { get; }
    public ICommand SelectLanguageCommand { get; }

    public ThemeCustomizationViewModel(ThemeService themeService, LocalizationService localization)
    {
        _themeService = themeService;
        _localization = localization;

        SelectLanguageCommand = new RelayCommand<AppLanguage>(lang =>
        {
            _localization.ApplyLanguage(lang);
            NotifyState();
        });

        SelectThemeCommand = new RelayCommand<AppThemeMode>(theme =>
        {
            _themeService.ApplyTheme(theme, _themeService.CurrentAccent);
            NotifyState();
        });

        SelectAccentCommand = new RelayCommand<AccentColorOption>(accent =>
        {
            _themeService.ApplyTheme(_themeService.CurrentTheme, accent);
            NotifyState();
        });

        BuildCollections();
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(CurrentTheme));
        OnPropertyChanged(nameof(CurrentAccent));
        OnPropertyChanged(nameof(CurrentLanguage));
        OnPropertyChanged(nameof(IsRussian));
        OnPropertyChanged(nameof(IsEnglish));
        OnPropertyChanged(nameof(IsDarkTheme));
        OnPropertyChanged(nameof(IsLightTheme));
        OnPropertyChanged(nameof(IsMidnightTheme));
    }

    private void BuildCollections()
    {
        Themes.Add(new ThemeItem
        {
            Mode = AppThemeMode.Dark,
            Title = "Тёмная (Dark)",
            Description = "Классический мягкий тёмный интерфейс WinTime",
            Icon = "🌙",
            CardBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E2E")),
            CardBorder = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E2E3E")),
            TextColor = Brushes.White
        });

        Themes.Add(new ThemeItem
        {
            Mode = AppThemeMode.Light,
            Title = "Светлая (Light)",
            Description = "Чистый и ясный светлый дизайн для дневного использования",
            Icon = "☀️",
            CardBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")),
            CardBorder = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D5DB")),
            TextColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111827"))
        });

        Themes.Add(new ThemeItem
        {
            Mode = AppThemeMode.Midnight,
            Title = "Полночь (Midnight)",
            Description = "Глубокий контрастный чёрный цвет для OLED экранов",
            Icon = "🌌",
            CardBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#14141E")),
            CardBorder = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#232332")),
            TextColor = Brushes.White
        });

        Accents.Add(new AccentItem
        {
            Option = AccentColorOption.Indigo,
            Name = "Индиго (Indigo)",
            HexColor = "#6366F1",
            ColorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6366F1"))
        });

        Accents.Add(new AccentItem
        {
            Option = AccentColorOption.Emerald,
            Name = "Изумруд (Emerald)",
            HexColor = "#10B981",
            ColorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"))
        });

        Accents.Add(new AccentItem
        {
            Option = AccentColorOption.Sky,
            Name = "Сапфир (Sky)",
            HexColor = "#0EA5E9",
            ColorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0EA5E9"))
        });

        Accents.Add(new AccentItem
        {
            Option = AccentColorOption.Rose,
            Name = "Роза (Rose)",
            HexColor = "#F43F5E",
            ColorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F43F5E"))
        });

        Accents.Add(new AccentItem
        {
            Option = AccentColorOption.Amber,
            Name = "Янтарь (Amber)",
            HexColor = "#F59E0B",
            ColorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"))
        });

        Accents.Add(new AccentItem
        {
            Option = AccentColorOption.Purple,
            Name = "Пурпур (Purple)",
            HexColor = "#A855F7",
            ColorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A855F7"))
        });
    }
}

