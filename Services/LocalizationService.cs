using System.Globalization;
using System.Windows;

namespace WinTime.Services;

public enum AppLanguage
{
    Ru,
    En
}

/// <summary>
/// Localization service providing dynamic UI language switching (Russian and English).
/// </summary>
public sealed class LocalizationService
{
    private readonly SettingsService _settings;
    private readonly Dictionary<string, string> _ruStrings = new();
    private readonly Dictionary<string, string> _enStrings = new();

    public AppLanguage CurrentLanguage { get; private set; } = AppLanguage.Ru;
    public event EventHandler? LanguageChanged;

    public LocalizationService(SettingsService settings)
    {
        _settings = settings;
        InitializeDictionaries();
    }

    public void Initialize()
    {
        if (Enum.TryParse<AppLanguage>(_settings.Language, true, out var lang))
            CurrentLanguage = lang;
        else
            CurrentLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ru", StringComparison.OrdinalIgnoreCase) 
                ? AppLanguage.Ru 
                : AppLanguage.En;

        ApplyLanguage(CurrentLanguage, saveSettings: false);
    }

    public void ApplyLanguage(AppLanguage language, bool saveSettings = true)
    {
        CurrentLanguage = language;
        if (saveSettings)
        {
            _settings.Language = language.ToString();
        }

        var targetDict = language == AppLanguage.Ru ? _ruStrings : _enStrings;
        var res = Application.Current?.Resources;
        if (res != null)
        {
            foreach (var kvp in targetDict)
            {
                res[kvp.Key] = kvp.Value;
            }
        }

        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public string GetString(string key)
    {
        var dict = CurrentLanguage == AppLanguage.Ru ? _ruStrings : _enStrings;
        return dict.TryGetValue(key, out var val) ? val : key;
    }

    private void InitializeDictionaries()
    {
        // Russian
        _ruStrings["Nav_Dashboard"]        = "📊   Дашборд";
        _ruStrings["Nav_Processes"]        = "💻   Процессы";
        _ruStrings["Nav_Applications"]     = "🗂   Приложения";
        _ruStrings["Nav_Profile"]          = "🏆   Профиль";
        _ruStrings["Nav_Widget"]           = "📌   Виджет";
        _ruStrings["Nav_Theme"]            = "🎨   Оформление";
        _ruStrings["Nav_Settings"]         = "⚙   Настройки";
        _ruStrings["Nav_PauseTracking"]    = "⏸  Приостановить";
        _ruStrings["Nav_ResumeTracking"]   = "▶  Возобновить";
        _ruStrings["App_Subtitle"]         = "Учёт экранного времени";

        _ruStrings["Theme_Title"]          = "Оформление и темы";
        _ruStrings["Theme_Subtitle"]       = "Настройте внешний вид WinTime: выбор темы оформления, языка и акцентных цветов";
        _ruStrings["Theme_LanguageSection"] = "ЯЗЫК ИНТЕРФЕЙСА (LANGUAGE)";
        _ruStrings["Theme_ModeSection"]     = "РЕЖИМ ОФОРМЛЕНИЯ";
        _ruStrings["Theme_AccentSection"]   = "ОСНОВНОЙ ЦВЕТ АКЦЕНТА";
        _ruStrings["Theme_PreviewSection"]  = "ИНТЕРАКТИВНЫЙ ПРЕДПРОСМОТР (LIVE PREVIEW)";

        _ruStrings["Theme_DarkTitle"]      = "Тёмная (Dark)";
        _ruStrings["Theme_DarkDesc"]       = "Классическая тёмная палитра для комфортной работы";
        _ruStrings["Theme_LightTitle"]     = "Светлая (Light)";
        _ruStrings["Theme_LightDesc"]      = "Чистый дизайн для дневного света и светлых интерьеров";
        _ruStrings["Theme_MidnightTitle"]  = "Полночь (Midnight)";
        _ruStrings["Theme_MidnightDesc"]   = "Глубокий контрастный чёрный цвет";
        _ruStrings["Theme_BtnSelect"]      = "Выбрать";
        _ruStrings["Theme_AccentHint"]     = "Выберите цвет кнопок, графиков и ключевых индикаторов:";

        _ruStrings["Preview_CardTitle"]    = "Пример карточки активности";
        _ruStrings["Preview_ActiveBadge"]  = "🔥 Активен";
        _ruStrings["Preview_TimeLabel"]    = "Общее время за сегодня";
        _ruStrings["Preview_TimeValue"]    = "6ч 42м";
        _ruStrings["Preview_Trend"]        = "▲ на 24% больше, чем вчера";
        _ruStrings["Preview_TopApp"]       = "Топ приложение: Visual Studio";
        _ruStrings["Preview_AccentBtn"]    = "Кнопка с акцентом";
        _ruStrings["Preview_SecondaryBtn"] = "Вторичная кнопка";

        _ruStrings["Setup_Title"]          = "Добро пожаловать в WinTime";
        _ruStrings["Setup_SelectLanguage"] = "Выберите язык интерфейса / Select language:";
        _ruStrings["Setup_Desc"]           = "Выберите файл, в котором WinTime будет хранить базу данных.\nЕсли файл не существует — он будет создан автоматически.";
        _ruStrings["Setup_PathEmpty"]      = "Путь не выбран…";
        _ruStrings["Setup_Browse"]         = "Обзор…";
        _ruStrings["Setup_Start"]          = "Начать отслеживание  →";

        // English
        _enStrings["Nav_Dashboard"]        = "📊   Dashboard";
        _enStrings["Nav_Processes"]        = "💻   Processes";
        _enStrings["Nav_Applications"]     = "🗂   Applications";
        _enStrings["Nav_Profile"]          = "🏆   Profile";
        _enStrings["Nav_Widget"]           = "📌   Widget";
        _enStrings["Nav_Theme"]            = "🎨   Appearance";
        _enStrings["Nav_Settings"]         = "⚙   Settings";
        _enStrings["Nav_PauseTracking"]    = "⏸  Pause Tracking";
        _enStrings["Nav_ResumeTracking"]   = "▶  Resume Tracking";
        _enStrings["App_Subtitle"]         = "Screen time & process tracker";

        _enStrings["Theme_Title"]          = "Appearance & Themes";
        _enStrings["Theme_Subtitle"]       = "Customize WinTime look and feel: theme mode, language, and accent colors";
        _enStrings["Theme_LanguageSection"] = "INTERFACE LANGUAGE";
        _enStrings["Theme_ModeSection"]     = "THEME MODE";
        _enStrings["Theme_AccentSection"]   = "PRIMARY ACCENT COLOR";
        _enStrings["Theme_PreviewSection"]  = "LIVE INTERACTIVE PREVIEW";

        _enStrings["Theme_DarkTitle"]      = "Dark";
        _enStrings["Theme_DarkDesc"]       = "Classic dark palette optimized for comfortable viewing";
        _enStrings["Theme_LightTitle"]     = "Light";
        _enStrings["Theme_LightDesc"]      = "Clean modern light design for daytime and bright rooms";
        _enStrings["Theme_MidnightTitle"]  = "Midnight";
        _enStrings["Theme_MidnightDesc"]   = "Ultra-deep pure black contrast";
        _enStrings["Theme_BtnSelect"]      = "Select";
        _enStrings["Theme_AccentHint"]     = "Choose your favorite accent color for buttons, charts, and highlights:";

        _enStrings["Preview_CardTitle"]    = "Sample Activity Card";
        _enStrings["Preview_ActiveBadge"]  = "🔥 Active";
        _enStrings["Preview_TimeLabel"]    = "Total Screen Time Today";
        _enStrings["Preview_TimeValue"]    = "6h 42m";
        _enStrings["Preview_Trend"]        = "▲ 24% more than yesterday";
        _enStrings["Preview_TopApp"]       = "Top Application: Visual Studio";
        _enStrings["Preview_AccentBtn"]    = "Accent Button";
        _enStrings["Preview_SecondaryBtn"] = "Secondary Button";

        _enStrings["Setup_Title"]          = "Welcome to WinTime";
        _enStrings["Setup_SelectLanguage"] = "Select interface language:";
        _enStrings["Setup_Desc"]           = "Choose where WinTime will store your SQLite database.\nIf the file does not exist, it will be created automatically.";
        _enStrings["Setup_PathEmpty"]      = "No path selected…";
        _enStrings["Setup_Browse"]         = "Browse…";
        _enStrings["Setup_Start"]          = "Start Tracking  →";
    }
}

