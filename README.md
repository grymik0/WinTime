<div align="center">
  <h1>⏱ WinTime</h1>
  <p><strong>Lightweight, privacy-first screen time, process, and productivity tracker for Windows</strong></p>
  <p>Know exactly where your time goes — by application, browser tab, window title, project, and daily rhythms.</p>

  <p>
    <a href="README.md"><strong>English</strong></a> • 
    <a href="README.ru.md"><strong>Русский</strong></a>
  </p>

  ![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)
  ![Platform](https://img.shields.io/badge/Platform-Windows-0078D4?style=flat-square&logo=windows)
  ![License](https://img.shields.io/badge/License-MIT-22c55e?style=flat-square)
  ![SQLite](https://img.shields.io/badge/Database-SQLite-003B57?style=flat-square&logo=sqlite)
  ![Localization](https://img.shields.io/badge/Language-EN%20%7C%20RU-blue?style=flat-square)

  <br/>

</div>

---

## What is WinTime?

WinTime runs quietly in your Windows system tray, tracking foreground window activity, process uptime, detailed browser tabs, and projects without slowing down your system. 

No cloud sync, no telemetry, and no account required — **100% of your data stays in a local SQLite database on your computer**.

---

## Key Features

| Feature | Details |
|---|---|
| 🎯 **Goals & Daily App Limits** | Set daily screen time limits for apps (hours & minutes). Choose between periodic reminder notifications or automated process closure with a 60-second warning dialog. |
| 📁 **Projects & Smart Categorization** | Organize your time into color-coded projects with custom icons. Create automated rules by process name or window title keyword/wildcard matching. |
| 🌐 **Full Bilingual Support** | Seamless dynamic language switching between **English** and **Russian (Русский)** across all screens, dialogs, and notifications without restarting. |
| 💫 **Fluent Transitions & Custom Titlebar** | Windows 11 / macOS style silky-smooth Zoom & Fade transitions (240ms) between views and dialogs. Frameless window with custom titlebar controls, active navigation indicators, and F11 fullscreen mode. |
| 📌 **Customizable Desktop Widget & Micro-Mode** | Draggable desktop overlay with live screen time, active app, session timer, and mouse metrics. Includes one-click Micro Mode (compact pill), opacity slider, click-through, and topmost options. |
| 🎨 **Theme & Accent Customization** | Light, Dark, and Midnight (OLED) themes with live interactive preview. Choose between 6 vibrant accent colors (Indigo, Emerald, Sky, Rose, Amber, Purple) applied app-wide. |
| 🏆 **Gamification, Levels & Daily Quests** | Earn XP for productive screen time (10 XP/min), level up from Novice to Grandmaster, complete daily quests, and unlock 12 tiered achievement badges. |
| 📅 **Weekly Recap Digest** | Automated weekly productivity report summarizing screen time, top apps, mouse statistics, goal compliance, and streak records. |
| 🌙 **Sleep & Daily Rhythm Analysis** | Syncs with Windows Event Log system power events (boot, sleep, hibernate, wake) and circular math to calculate your sleep, wake-up times, and rest duration. |
| 🖱 **Mouse Activity (Clicks & Distance)** | High-frequency background tracking of mouse clicks and cursor distance traveled in meters/kilometers with jump rejection. |
| 🎮 **Gaming Mode & Detection** | Automatic recognition of games and launchers (Steam, Epic Games, Riot, etc.) with dedicated category filtering. |
| 🔍 **Browser Tabs & Window Titles** | Deep tracking for active browser tabs (Chrome, Edge, Opera, etc.) and code editors (VS Code, Visual Studio, JetBrains). Expand any app to see exact per-tab time spent. |
| 📅 **Activity Heatmap** | GitHub-style 20-week contribution grid displaying your daily active hours, streaks, best productive days, and consistency metrics. |
| 📊 **Trends & Period Comparisons** | Compare your screen time against the previous period (*Today vs. Yesterday, This Week vs. Last Week, This Month vs. Last Month*) with exact diffs and percentages. |
| ⚡ **Process Uptime & Live Counters** | Monitor background and running Windows processes with real-time live ticking counters and launch timestamps. |
| ⏱ **Automatic Activity & AFK Detection** | Accurately polls foreground windows every second while marking idle/inactive periods when mouse and keyboard input cease. |
| 📈 **Interactive Visual Dashboard** | Clean Fluent Design UI featuring summary cards, top 5 donut breakdown, and adaptive hourly/daily/monthly activity bar charts. |
| 🏷 **Applications & Category Management** | Browse, rename, customize, and categorize all tracked applications with high-resolution system icons. |
| 🚫 **Process Blacklist** | Exclude confidential apps or background utilities from being logged. |
| 💾 **Data Export** | Export your raw historical sessions and analytics directly to CSV or JSON formats at any time. |
| 🪟 **Tray Integration & Autostart** | Minimizes to system tray on close; optional Windows autorun on system startup. Single-instance guaranteed. |
| 🔒 **100% Offline & Private** | Zero network calls, zero trackers, zero cloud dependencies. Your data is stored locally in an embedded SQLite database. |

---

## Tech Stack

- **Framework**: WPF (.NET 10, C# 13)
- **Fluent UI & Theming**: Custom WPF Fluent Styles, `WindowChrome` Frameless Window, Dynamic Resource Dictionaries
- **Charts**: [LiveChartsCore](https://livecharts.dev/) 2.0.5 (SkiaSharp)
- **Database**: SQLite via [Microsoft.Data.Sqlite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/) 10.0 + [Dapper](https://github.com/DapperLib/Dapper) ORM
- **Tray Icon**: [Hardcodet.NotifyIcon.Wpf](https://github.com/hardcodet/wpf-notifyicon) 1.1
- **Exporting**: [CsvHelper](https://joshclose.github.io/CsvHelper/) 33
- **Architecture**: MVVM pattern (lightweight reactive properties, custom relay commands, modular service registry)

---

## Requirements

- **Operating System**: Windows 10 (version 1809+) or Windows 11 (x64)
- **Runtime**: [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) *(included if using self-contained builds)*

---

## Getting Started

### Run from Source

```bash
git clone
cd WinTime
dotnet run -p:Platform=x64
```
### First Launch

When opening WinTime for the first time, a setup dialog guides you to pick your preferred interface language (English or Russian) and a folder for storing your SQLite `.db` file (default: `%LOCALAPPDATA%\WinTime`). After selection, tracking begins automatically and the app docks to your Windows system tray.

---

## Project Structure

```
WinTime/
├── Core/               # ActivityTracker, AnimationHelper, Native Win32 APIs, StartupManager, InMemoryBuffer
├── Data/               # DatabaseService, ActivityRepository, ApplicationRepository, GoalRepository, ProjectRepository
├── Models/             # AppStatItem, WindowTitleStatItem, ProcessUptimeItem, GoalLimit, Project, AchievementItem
├── Services/           # LocalizationService, SettingsService, ThemeService, ExportService, IconService
├── ViewModels/         # Dashboard, Processes, Applications, Goals, Projects, Profile, DesktopWidget, Theme, Settings
├── Views/              # DashboardView, GoalsView, ProjectsView, ProfileView, DesktopWidgetWindow, Custom Dialogs
├── Converters/         # SecondsToTimeConverter, BooleanToVisibilityConverter
├── Assets/             # App icons and graphics
├── AppServices.cs      # Lightweight dependency injection / service registry
├── App.xaml.cs         # App lifecycle, system tray hooks, single-instance mutex
└── WinTime.csproj      # .NET 10 project definition
```

---

## Privacy Policy

WinTime was created with privacy as its primary foundation:
- **No telemetry** or usage tracking.
- **No crash dumps** uploaded to external servers.
- **No internet requests** are ever performed by the application.
- All window titles, timestamps, and uptime logs remain exclusively on your local storage.

---

