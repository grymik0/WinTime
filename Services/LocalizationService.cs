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

    public static LocalizationService? Instance { get; private set; }

    public LocalizationService(SettingsService settings)
    {
        _settings = settings;
        Instance  = this;
        InitializeDictionaries();
    }

    public static bool IsRussian => Instance?.CurrentLanguage != AppLanguage.En;

    public static string FormatDuration(long totalSec)
    {
        bool isRu = IsRussian;
        string hUnit = isRu ? "ч" : "h";
        string mUnit = isRu ? "м" : "m";
        string sUnit = isRu ? "с" : "s";

        if (totalSec <= 0) return $"0{sUnit}";
        var h = totalSec / 3600;
        var m = (totalSec % 3600) / 60;
        var s = totalSec % 60;

        if (h > 0) return $"{h}{hUnit} {m}{mUnit}";
        if (m > 0) return $"{m}{mUnit} {s}{sUnit}";
        return $"{s}{sUnit}";
    }

    public static string FormatDurationFull(long totalSec)
    {
        bool isRu = IsRussian;
        string hUnit = isRu ? "ч" : "h";
        string mUnit = isRu ? "м" : "m";
        string sUnit = isRu ? "с" : "s";

        var ts = TimeSpan.FromSeconds(Math.Max(0, totalSec));
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}{hUnit} {ts.Minutes:D2}{mUnit}";
        if (ts.TotalMinutes >= 1) return $"{ts.Minutes}{mUnit} {ts.Seconds:D2}{sUnit}";
        return $"{ts.Seconds}{sUnit}";
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
        // ==========================================
        // RUSSIAN DICTIONARY
        // ==========================================

        // Navigation & General App
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
        _ruStrings["Common_Loading"]       = "Загрузка…";
        _ruStrings["Common_Refresh"]       = "🔄  Обновить";
        _ruStrings["Common_Today"]         = "Сегодня";
        _ruStrings["Common_Week"]          = "Неделя";
        _ruStrings["Common_Month"]         = "Месяц";

        // Dashboard
        _ruStrings["Dash_Title"]           = "Дашборд";
        _ruStrings["Dash_ScreenTime"]      = "⏱  Экранное время";
        _ruStrings["Dash_AfkTime"]         = "💤  Время AFK";
        _ruStrings["Dash_TopApp"]          = "🏆  Топ приложение";
        _ruStrings["Dash_MouseActivity"]   = "🖱  Активность мыши";
        _ruStrings["Dash_Clicks"]          = "кликов";
        _ruStrings["Dash_Distance"]        = "дистанция";
        _ruStrings["Dash_HeatmapTitle"]    = "📅  Календарь активности";
        _ruStrings["Dash_HeatmapSubtitle"] = "(последние 20 недель)";
        _ruStrings["Dash_HeatmapLess"]     = "Меньше";
        _ruStrings["Dash_HeatmapMore"]     = "Больше";
        _ruStrings["Dash_ConsistencyTitle"]= "Анализ постоянства";
        _ruStrings["Dash_Total20Weeks"]    = "Всего за 20 нед.";
        _ruStrings["Dash_Regularity"]      = "Регулярность";
        _ruStrings["Dash_InActiveDay"]     = "В активный день";
        _ruStrings["Dash_BestDay"]         = "Лучший день";
        _ruStrings["Dash_RhythmTitle"]     = "🌙  Режим дня и сон";
        _ruStrings["Dash_RhythmSubtitle"]  = "(по времени активности ПК)";
        _ruStrings["Dash_RhythmWakeUp"]    = "🌅  Первое включение (подъём)";
        _ruStrings["Dash_RhythmSleep"]     = "🌃  Последнее выключение (отбой)";
        _ruStrings["Dash_RhythmBreak"]     = "💤  Ночной перерыв (сон/отдых)";
        _ruStrings["Dash_MonthlyAvg"]      = "в среднем за месяц";
        _ruStrings["Dash_PcOffInterval"]   = "интервал выключенного ПК";
        _ruStrings["Dash_AppsSection"]     = "Приложения";
        _ruStrings["Dash_NoAppsData"]      = "Данных пока нет. Начните работу — статистика появится здесь.";
        _ruStrings["Dash_Others"]          = "Остальные";
        _ruStrings["Dash_ActiveHoursChart"]= "Активное время (ч)";
        _ruStrings["Dash_DayWord"]         = "дн.";

        // Processes View
        _ruStrings["Proc_Title"]           = "Время работы процессов";
        _ruStrings["Proc_Subtitle"]        = "Отслеживание всех запущенных окон: общее время в системе против активного фокуса";
        _ruStrings["Proc_AllProcesses"]    = "Все процессы";
        _ruStrings["Proc_GameMode"]        = "🎮  Игровой режим";
        _ruStrings["Proc_SearchPrompt"]    = "Поиск процесса…";
        _ruStrings["Proc_ColStatus"]       = "Статус";
        _ruStrings["Proc_ColApp"]          = "Приложение";
        _ruStrings["Proc_ColProcess"]      = "Процесс";
        _ruStrings["Proc_ColUptime"]       = "Время работы";
        _ruStrings["Proc_ColActive"]       = "В фокусе";
        _ruStrings["Proc_ColRatio"]        = "% активности";
        _ruStrings["Proc_RowDetailsTitle"] = "📄  Вкладки и окна";
        _ruStrings["Proc_RowDetailsSub"]   = "(детализация за выбранный период)";
        _ruStrings["Proc_RowDetailsLoading"] = "Загрузка заголовков…";
        _ruStrings["Proc_RowDetailsEmpty"] = "Нет отдельных заголовков окон за выбранный период";
        _ruStrings["Proc_ExpandTooltip"]   = "Показать вкладки и заголовки окон";
        _ruStrings["Proc_StatusRunning"]   = "Работает";
        _ruStrings["Proc_StatusClosed"]    = "Закрыто";

        // Applications View
        _ruStrings["Apps_Title"]           = "Приложения";
        _ruStrings["Apps_ColApp"]          = "Приложение";
        _ruStrings["Apps_ColProcess"]      = "Процесс";
        _ruStrings["Apps_ColCategory"]     = "Категория";
        _ruStrings["Apps_ColBlacklist"]    = "Не отслеживать";

        // Profile View
        _ruStrings["Profile_Title"]        = "Профиль и Достижения";
        _ruStrings["Profile_Subtitle"]     = "Ваш прогресс активности, уровни опыта и разблокированные награды";
        _ruStrings["Profile_XpHint"]       = "Зарабатывайте XP, проводя время за полезной работой";
        _ruStrings["Profile_AllTime"]      = "Время за всё время";
        _ruStrings["Profile_ActiveDays"]   = "Активных дней";
        _ruStrings["Profile_Achievements"] = "Достижения";
        _ruStrings["Profile_Progress"]     = "Прогресс уровня";
        _ruStrings["Profile_AllAchList"]   = "🏆  Все достижения";

        // Desktop Widget Settings View
        _ruStrings["Widget_Title"]         = "Виджет на рабочий стол";
        _ruStrings["Widget_Subtitle"]      = "Настройка плавающего мини-информера поверх окон и рабочего стола";
        _ruStrings["Widget_StateSection"]  = "Состояние виджета";
        _ruStrings["Widget_ScreenDisplay"] = "Отображение на экране Windows";
        _ruStrings["Widget_StatusOff"]     = "ОТКЛЮЧЁН";
        _ruStrings["Widget_StatusOn"]      = "АКТИВЕН";
        _ruStrings["Widget_BtnEnable"]     = "Включить виджет";
        _ruStrings["Widget_BtnDisable"]    = "Выключить виджет";
        _ruStrings["Widget_CompactBtn"]    = "📐  Компактный вид";
        _ruStrings["Widget_ExpandedBtn"]   = "📖  Развёрнутый вид";
        _ruStrings["Widget_ResetPos"]      = "📍  Сбросить позицию";
        _ruStrings["Widget_ResetPosTip"]   = "Вернуть виджет в правый верхний угол экрана";
        _ruStrings["Widget_SectionBehavior"] = "ПОВЕДЕНИЕ И ВЗАИМОДЕЙСТВИЕ";
        _ruStrings["Widget_ClickThrough"]  = "🔒  Пропускать клики сквозь виджет (Click-through)";
        _ruStrings["Widget_ClickThroughDesc"] = "Курсор проходит насквозь в окна позади него, не мешая работе и играм";
        _ruStrings["Widget_Topmost"]       = "📌  Всегда поверх всех окон";
        _ruStrings["Widget_TopmostDesc"]   = "Если выключить — виджет останется на рабочем столе и скроется под окнами";
        _ruStrings["Widget_SectionBlocks"] = "ОТОБРАЖАЕМЫЕ БЛОКИ";
        _ruStrings["Widget_ShowApp"]       = "Текущее активное приложение";
        _ruStrings["Widget_ShowAppDesc"]   = "Название программы в фокусе или статус бездействия (AFK)";
        _ruStrings["Widget_ShowTime"]      = "Экранное время за сегодня";
        _ruStrings["Widget_ShowTimeDesc"]  = "Общее время активного использования компьютера";
        _ruStrings["Widget_ShowSession"]   = "Таймер непрерывной текущей сессии";
        _ruStrings["Widget_ShowSessionDesc"] = "Время непрерывной работы без отдыха (сбрасывается при уходе в AFK)";
        _ruStrings["Widget_ShowLevel"]     = "Уровень профиля и опыт (XP)";
        _ruStrings["Widget_ShowLevelDesc"] = "Ранг, текущий уровень и прогресс до следующего уровня";
        _ruStrings["Widget_ShowMouse"]     = "Активность мыши";
        _ruStrings["Widget_ShowMouseDesc"] = "Количество кликов и пройденная дистанция курсора в реальном времени";
        _ruStrings["Widget_SectionAppearance"] = "ВНЕШНИЙ ВИД И ПРОЗРАЧНОСТЬ";
        _ruStrings["Widget_Opacity"]       = "Непрозрачность окна";
        _ruStrings["Widget_PreviewSection"]= "ПРЕДПРОСМОТР ВИДЖЕТА";

        // Floating Desktop Widget Window
        _ruStrings["Widget_InFocus"]       = "В фокусе";
        _ruStrings["Widget_TodayPc"]       = "Сегодня за ПК";
        _ruStrings["Widget_CurSession"]    = "Текущая сессия";
        _ruStrings["Widget_TipExpand"]     = "Развернуть полный вид";
        _ruStrings["Widget_TipCollapse"]   = "Свернуть в компактную полоску";
        _ruStrings["Widget_TipHide"]       = "Скрыть виджет";

        // Settings View
        _ruStrings["Settings_Title"]       = "Настройки";
        _ruStrings["Settings_Subtitle"]    = "Конфигурация WinTime";
        _ruStrings["Settings_SectionTracking"] = "ОТСЛЕЖИВАНИЕ";
        _ruStrings["Settings_AfkThreshold"]= "Порог бездействия (AFK)";
        _ruStrings["Settings_AfkUnit"]     = "мин";
        _ruStrings["Settings_AfkHint"]     = "Если в течение этого времени нет ввода с мыши/клавиатуры — время считается AFK.";
        _ruStrings["Settings_SectionSystem"] = "СИСТЕМА";
        _ruStrings["Settings_LaunchStartup"] = "Запускать WinTime при старте Windows";
        _ruStrings["Settings_SectionDb"]   = "БАЗА ДАННЫХ";
        _ruStrings["Settings_DbPath"]      = "Путь к файлу базы данных";
        _ruStrings["Settings_DbBrowse"]    = "Обзор…";
        _ruStrings["Settings_SectionExport"] = "ЭКСПОРТ ДАННЫХ";
        _ruStrings["Settings_ExportCsv"]   = "📥  Экспорт в CSV";
        _ruStrings["Settings_ExportJson"]  = "📥  Экспорт в JSON";
        _ruStrings["Settings_SectionDanger"] = "ОПАСНАЯ ЗОНА";
        _ruStrings["Settings_ClearHistory"]= "🗑  Очистить историю активности";
        _ruStrings["Settings_ClearHint"]   = "Удаляет все записи об активности. Список приложений остаётся.";

        // Theme View
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

        // Preview Card
        _ruStrings["Preview_CardTitle"]    = "Пример карточки активности";
        _ruStrings["Preview_ActiveBadge"]  = "🔥 Активен";
        _ruStrings["Preview_TimeLabel"]    = "Общее время за сегодня";
        _ruStrings["Preview_TimeValue"]    = "6ч 42м";
        _ruStrings["Preview_Trend"]        = "▲ на 24% больше, чем вчера";
        _ruStrings["Preview_TopApp"]       = "Топ приложение: Visual Studio";
        _ruStrings["Preview_AccentBtn"]    = "Кнопка с акцентом";
        _ruStrings["Preview_SecondaryBtn"] = "Вторичная кнопка";

        // Setup / First Run
        _ruStrings["Setup_Title"]          = "Добро пожаловать в WinTime";
        _ruStrings["Setup_SelectLanguage"] = "Выберите язык интерфейса / Select language:";
        _ruStrings["Setup_Desc"]           = "Выберите файл, в котором WinTime будет хранить базу данных.\nЕсли файл не существует — он будет создан автоматически.";
        _ruStrings["Setup_PathEmpty"]      = "Путь не выбран…";
        _ruStrings["Setup_Browse"]         = "Обзор…";
        _ruStrings["Setup_Start"]          = "Начать отслеживание  →";


        // ==========================================
        // ENGLISH DICTIONARY
        // ==========================================

        // Navigation & General App
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
        _enStrings["Common_Loading"]       = "Loading…";
        _enStrings["Common_Refresh"]       = "🔄  Refresh";
        _enStrings["Common_Today"]         = "Today";
        _enStrings["Common_Week"]          = "Week";
        _enStrings["Common_Month"]         = "Month";

        // Dashboard
        _enStrings["Dash_Title"]           = "Dashboard";
        _enStrings["Dash_ScreenTime"]      = "⏱  Screen Time";
        _enStrings["Dash_AfkTime"]         = "💤  AFK Time";
        _enStrings["Dash_TopApp"]          = "🏆  Top Application";
        _enStrings["Dash_MouseActivity"]   = "🖱  Mouse Activity";
        _enStrings["Dash_Clicks"]          = "clicks";
        _enStrings["Dash_Distance"]        = "distance";
        _enStrings["Dash_HeatmapTitle"]    = "📅  Activity Calendar";
        _enStrings["Dash_HeatmapSubtitle"] = "(past 20 weeks)";
        _enStrings["Dash_HeatmapLess"]     = "Less";
        _enStrings["Dash_HeatmapMore"]     = "More";
        _enStrings["Dash_ConsistencyTitle"]= "Consistency Analysis";
        _enStrings["Dash_Total20Weeks"]    = "Total in 20 wks";
        _enStrings["Dash_Regularity"]      = "Regularity";
        _enStrings["Dash_InActiveDay"]     = "On active day";
        _enStrings["Dash_BestDay"]         = "Best day";
        _enStrings["Dash_RhythmTitle"]     = "🌙  Daily Rhythm & Sleep";
        _enStrings["Dash_RhythmSubtitle"]  = "(by PC activity periods)";
        _enStrings["Dash_RhythmWakeUp"]    = "🌅  First wake-up (start)";
        _enStrings["Dash_RhythmSleep"]     = "🌃  Last bedtime (shutdown)";
        _enStrings["Dash_RhythmBreak"]     = "💤  Night gap (sleep/rest)";
        _enStrings["Dash_MonthlyAvg"]      = "monthly average";
        _enStrings["Dash_PcOffInterval"]   = "PC turned off interval";
        _enStrings["Dash_AppsSection"]     = "Applications";
        _enStrings["Dash_NoAppsData"]      = "No data yet. Start your workflow — stats will appear here.";
        _enStrings["Dash_Others"]          = "Others";
        _enStrings["Dash_ActiveHoursChart"]= "Active Time (h)";
        _enStrings["Dash_DayWord"]         = "days";

        // Processes View
        _enStrings["Proc_Title"]           = "Process Activity & Uptime";
        _enStrings["Proc_Subtitle"]        = "Track all running windows: total system presence vs active window focus";
        _enStrings["Proc_AllProcesses"]    = "All Processes";
        _enStrings["Proc_GameMode"]        = "🎮  Game Mode";
        _enStrings["Proc_SearchPrompt"]    = "Search processes…";
        _enStrings["Proc_ColStatus"]       = "Status";
        _enStrings["Proc_ColApp"]          = "Application";
        _enStrings["Proc_ColProcess"]      = "Process";
        _enStrings["Proc_ColUptime"]       = "Uptime";
        _enStrings["Proc_ColActive"]       = "In Focus";
        _enStrings["Proc_ColRatio"]        = "% Activity";
        _enStrings["Proc_RowDetailsTitle"] = "📄  Tabs & Windows";
        _enStrings["Proc_RowDetailsSub"]   = "(breakdown for selected period)";
        _enStrings["Proc_RowDetailsLoading"] = "Loading titles…";
        _enStrings["Proc_RowDetailsEmpty"] = "No window titles recorded for this period";
        _enStrings["Proc_ExpandTooltip"]   = "Show window titles and tabs";
        _enStrings["Proc_StatusRunning"]   = "Running";
        _enStrings["Proc_StatusClosed"]    = "Closed";

        // Applications View
        _enStrings["Apps_Title"]           = "Applications";
        _enStrings["Apps_ColApp"]          = "Application";
        _enStrings["Apps_ColProcess"]      = "Process";
        _enStrings["Apps_ColCategory"]     = "Category";
        _enStrings["Apps_ColBlacklist"]    = "Do not track";

        // Profile View
        _enStrings["Profile_Title"]        = "Profile & Achievements";
        _enStrings["Profile_Subtitle"]     = "Your activity milestones, XP progression, and unlocked awards";
        _enStrings["Profile_XpHint"]       = "Earn XP by spending focused time in productive applications";
        _enStrings["Profile_AllTime"]      = "All-Time Screen Time";
        _enStrings["Profile_ActiveDays"]   = "Active Days";
        _enStrings["Profile_Achievements"] = "Achievements";
        _enStrings["Profile_Progress"]     = "Level Progression";
        _enStrings["Profile_AllAchList"]   = "🏆  All Achievements";

        // Desktop Widget Settings View
        _enStrings["Widget_Title"]         = "Desktop Widget";
        _enStrings["Widget_Subtitle"]      = "Configure floating mini-widget overlay for desktop and windows";
        _enStrings["Widget_StateSection"]  = "Widget State";
        _enStrings["Widget_ScreenDisplay"] = "Windows screen display";
        _enStrings["Widget_StatusOff"]     = "DISABLED";
        _enStrings["Widget_StatusOn"]      = "ACTIVE";
        _enStrings["Widget_BtnEnable"]     = "Enable Widget";
        _enStrings["Widget_BtnDisable"]    = "Disable Widget";
        _enStrings["Widget_CompactBtn"]    = "📐  Compact View";
        _enStrings["Widget_ExpandedBtn"]   = "📖  Expanded View";
        _enStrings["Widget_ResetPos"]      = "📍  Reset Position";
        _enStrings["Widget_ResetPosTip"]   = "Move widget to top-right screen corner";
        _enStrings["Widget_SectionBehavior"] = "BEHAVIOR & INTERACTION";
        _enStrings["Widget_ClickThrough"]  = "🔒  Pass clicks through widget (Click-through)";
        _enStrings["Widget_ClickThroughDesc"] = "Mouse clicks pass directly into windows underneath without interruption";
        _enStrings["Widget_Topmost"]       = "📌  Always on Top";
        _enStrings["Widget_TopmostDesc"]   = "If unchecked, the widget stays on desktop and gets covered by windows";
        _enStrings["Widget_SectionBlocks"] = "DISPLAYED MODULES";
        _enStrings["Widget_ShowApp"]       = "Current Active App";
        _enStrings["Widget_ShowAppDesc"]   = "Display app name or AFK idle status";
        _enStrings["Widget_ShowTime"]      = "Screen Time Today";
        _enStrings["Widget_ShowTimeDesc"]  = "Total active PC usage today";
        _enStrings["Widget_ShowSession"]   = "Continuous Session Timer";
        _enStrings["Widget_ShowSessionDesc"] = "Time worked continuously without breaks (resets on AFK)";
        _enStrings["Widget_ShowLevel"]     = "Profile Level & XP";
        _enStrings["Widget_ShowLevelDesc"] = "Rank, current level, and progress to next level";
        _enStrings["Widget_ShowMouse"]     = "Mouse Activity";
        _enStrings["Widget_ShowMouseDesc"] = "Live clicks and distance counter";
        _enStrings["Widget_SectionAppearance"] = "APPEARANCE & TRANSPARENCY";
        _enStrings["Widget_Opacity"]       = "Window Opacity";
        _enStrings["Widget_PreviewSection"]= "WIDGET PREVIEW";

        // Floating Desktop Widget Window
        _enStrings["Widget_InFocus"]       = "In Focus";
        _enStrings["Widget_TodayPc"]       = "Today on PC";
        _enStrings["Widget_CurSession"]    = "Current Session";
        _enStrings["Widget_TipExpand"]     = "Expand to full view";
        _enStrings["Widget_TipCollapse"]   = "Collapse to compact strip";
        _enStrings["Widget_TipHide"]       = "Hide widget";

        // Settings View
        _enStrings["Settings_Title"]       = "Settings";
        _enStrings["Settings_Subtitle"]    = "WinTime Configuration";
        _enStrings["Settings_SectionTracking"] = "TRACKING";
        _enStrings["Settings_AfkThreshold"]= "AFK Inactivity Threshold";
        _enStrings["Settings_AfkUnit"]     = "min";
        _enStrings["Settings_AfkHint"]     = "Time without mouse/keyboard input before counting as AFK.";
        _enStrings["Settings_SectionSystem"] = "SYSTEM";
        _enStrings["Settings_LaunchStartup"] = "Start WinTime on Windows startup";
        _enStrings["Settings_SectionDb"]   = "DATABASE";
        _enStrings["Settings_DbPath"]      = "Database file path";
        _enStrings["Settings_DbBrowse"]    = "Browse…";
        _enStrings["Settings_SectionExport"] = "EXPORT DATA";
        _enStrings["Settings_ExportCsv"]   = "📥  Export to CSV";
        _enStrings["Settings_ExportJson"]  = "📥  Export to JSON";
        _enStrings["Settings_SectionDanger"] = "DANGER ZONE";
        _enStrings["Settings_ClearHistory"]= "🗑  Clear Activity History";
        _enStrings["Settings_ClearHint"]   = "Permanently clears all activity records. Applications list is kept.";

        // Theme View
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

        // Preview Card
        _enStrings["Preview_CardTitle"]    = "Sample Activity Card";
        _enStrings["Preview_ActiveBadge"]  = "🔥 Active";
        _enStrings["Preview_TimeLabel"]    = "Total Screen Time Today";
        _enStrings["Preview_TimeValue"]    = "6h 42m";
        _enStrings["Preview_Trend"]        = "▲ 24% more than yesterday";
        _enStrings["Preview_TopApp"]       = "Top Application: Visual Studio";
        _enStrings["Preview_AccentBtn"]    = "Accent Button";
        _enStrings["Preview_SecondaryBtn"] = "Secondary Button";

        // Setup / First Run
        _enStrings["Setup_Title"]          = "Welcome to WinTime";
        _enStrings["Setup_SelectLanguage"] = "Select interface language:";
        _enStrings["Setup_Desc"]           = "Choose where WinTime will store your SQLite database.\nIf the file does not exist, it will be created automatically.";
        _enStrings["Setup_PathEmpty"]      = "No path selected…";
        _enStrings["Setup_Browse"]         = "Browse…";
        _enStrings["Setup_Start"]          = "Start Tracking  →";
    }
}
