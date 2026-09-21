using System.Windows;
using System.Windows.Media;

namespace WinTime.Services;

public enum AppThemeMode
{
    Dark,
    Light,
    Midnight
}

public enum AccentColorOption
{
    Indigo,
    Emerald,
    Sky,
    Rose,
    Amber,
    Purple
}

public sealed class ThemeService
{
    private readonly SettingsService _settings;

    public AppThemeMode CurrentTheme { get; private set; } = AppThemeMode.Dark;
    public AccentColorOption CurrentAccent { get; private set; } = AccentColorOption.Indigo;

    public event EventHandler? ThemeChanged;

    public ThemeService(SettingsService settings)
    {
        _settings = settings;
    }

    public void Initialize()
    {
        if (Enum.TryParse<AppThemeMode>(_settings.ThemeMode, true, out var t))
            CurrentTheme = t;
        else
            CurrentTheme = AppThemeMode.Dark;

        if (Enum.TryParse<AccentColorOption>(_settings.AccentColor, true, out var a))
            CurrentAccent = a;
        else
            CurrentAccent = AccentColorOption.Indigo;

        ApplyTheme(CurrentTheme, CurrentAccent, saveSettings: false);
    }

    public void ApplyTheme(AppThemeMode theme, AccentColorOption accent, bool saveSettings = true)
    {
        CurrentTheme = theme;
        CurrentAccent = accent;

        if (saveSettings)
        {
            _settings.ThemeMode = theme.ToString();
            _settings.AccentColor = accent.ToString();
        }

        var res = Application.Current?.Resources;
        if (res == null) return;

        // 1. Цвета темы (Фоны, границы, тексты)
        Color appBgColor;
        Color sidebarBgColor;
        Color cardBgColor;
        Color cardSubBgColor;
        Color controlBgColor;
        Color controlHoverBgColor;
        Color borderColor;
        Color textPrimaryColor;
        Color textSecondaryColor;
        Color textMutedColor;

        switch (theme)
        {
            case AppThemeMode.Light:
                appBgColor          = (Color)ColorConverter.ConvertFromString("#F3F4F6"); // Серый мягкий фон
                sidebarBgColor      = (Color)ColorConverter.ConvertFromString("#FFFFFF"); // Белый сайдбар
                cardBgColor         = (Color)ColorConverter.ConvertFromString("#FFFFFF"); // Белые карточки
                cardSubBgColor      = (Color)ColorConverter.ConvertFromString("#F9FAFB"); // Слегка серый под-фон
                controlBgColor      = (Color)ColorConverter.ConvertFromString("#E5E7EB");
                controlHoverBgColor = (Color)ColorConverter.ConvertFromString("#D1D5DB");
                borderColor         = (Color)ColorConverter.ConvertFromString("#E5E7EB");
                textPrimaryColor    = (Color)ColorConverter.ConvertFromString("#111827"); // Почти черный
                textSecondaryColor  = (Color)ColorConverter.ConvertFromString("#4B5563");
                textMutedColor      = (Color)ColorConverter.ConvertFromString("#9CA3AF");
                break;

            case AppThemeMode.Midnight:
                appBgColor          = (Color)ColorConverter.ConvertFromString("#0B0B0F"); // Глубокий черный OLED
                sidebarBgColor      = (Color)ColorConverter.ConvertFromString("#101017");
                cardBgColor         = (Color)ColorConverter.ConvertFromString("#14141E");
                cardSubBgColor      = (Color)ColorConverter.ConvertFromString("#0D0D14");
                controlBgColor      = (Color)ColorConverter.ConvertFromString("#1E1E2C");
                controlHoverBgColor = (Color)ColorConverter.ConvertFromString("#28283C");
                borderColor         = (Color)ColorConverter.ConvertFromString("#232332");
                textPrimaryColor    = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                textSecondaryColor  = (Color)ColorConverter.ConvertFromString("#9CA3AF");
                textMutedColor      = (Color)ColorConverter.ConvertFromString("#6B7280");
                break;

            case AppThemeMode.Dark:
            default:
                appBgColor          = (Color)ColorConverter.ConvertFromString("#16161E"); // Фирменный темно-синий
                sidebarBgColor      = (Color)ColorConverter.ConvertFromString("#1C1C28");
                cardBgColor         = (Color)ColorConverter.ConvertFromString("#1E1E2E");
                cardSubBgColor      = (Color)ColorConverter.ConvertFromString("#181825");
                controlBgColor      = (Color)ColorConverter.ConvertFromString("#252535");
                controlHoverBgColor = (Color)ColorConverter.ConvertFromString("#313147");
                borderColor         = (Color)ColorConverter.ConvertFromString("#252535");
                textPrimaryColor    = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                textSecondaryColor  = (Color)ColorConverter.ConvertFromString("#9CA3AF");
                textMutedColor      = (Color)ColorConverter.ConvertFromString("#6B7280");
                break;
        }

        // 2. Акцентный цвет
        string accentHex;
        string accentHoverHex;

        switch (accent)
        {
            case AccentColorOption.Emerald:
                accentHex = "#10B981";
                accentHoverHex = "#059669";
                break;
            case AccentColorOption.Sky:
                accentHex = "#0EA5E9";
                accentHoverHex = "#0284C7";
                break;
            case AccentColorOption.Rose:
                accentHex = "#F43F5E";
                accentHoverHex = "#E11D48";
                break;
            case AccentColorOption.Amber:
                accentHex = "#F59E0B";
                accentHoverHex = "#D97706";
                break;
            case AccentColorOption.Purple:
                accentHex = "#A855F7";
                accentHoverHex = "#9333EA";
                break;
            case AccentColorOption.Indigo:
            default:
                accentHex = "#6366F1";
                accentHoverHex = "#4F46E5";
                break;
        }

        var accentColor = (Color)ColorConverter.ConvertFromString(accentHex);
        var accentHoverColor = (Color)ColorConverter.ConvertFromString(accentHoverHex);

        // Обновляем DynamicResource словаря
        res["AppBgBrush"]          = new SolidColorBrush(appBgColor);
        res["SidebarBgBrush"]      = new SolidColorBrush(sidebarBgColor);
        res["CardBgBrush"]         = new SolidColorBrush(cardBgColor);
        res["CardSubBgBrush"]      = new SolidColorBrush(cardSubBgColor);
        res["ControlBgBrush"]      = new SolidColorBrush(controlBgColor);
        res["ControlHoverBgBrush"] = new SolidColorBrush(controlHoverBgColor);
        res["BorderBrushColor"]    = new SolidColorBrush(borderColor);
        res["TextPrimaryBrush"]    = new SolidColorBrush(textPrimaryColor);
        res["TextSecondaryBrush"]  = new SolidColorBrush(textSecondaryColor);
        res["TextMutedBrush"]      = new SolidColorBrush(textMutedColor);
        res["AccentBrush"]         = new SolidColorBrush(accentColor);
        res["AccentHoverBrush"]    = new SolidColorBrush(accentHoverColor);

        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }
}

