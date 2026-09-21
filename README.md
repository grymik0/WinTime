<div align="center">
  <h1>⏱ WinTime</h1>
  <p><strong>Lightweight, privacy-first screen time and process tracker for Windows</strong></p>
  <p>Know exactly where your time goes — by application, browser tab, window title, and hourly/daily trends.</p>

  ![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)
  ![Platform](https://img.shields.io/badge/Platform-Windows-0078D4?style=flat-square&logo=windows)
  ![License](https://img.shields.io/badge/License-MIT-22c55e?style=flat-square)
  ![SQLite](https://img.shields.io/badge/Database-SQLite-003B57?style=flat-square&logo=sqlite)

  <br/>

</div>

---

## What is WinTime?

WinTime runs quietly in your Windows system tray, tracking foreground window activity, process uptime, and detailed window titles / browser tabs without slowing down your system. 

No cloud sync, no telemetry, and no account required — **100% of your data stays in a local SQLite database on your computer**.

---

## Key Features

| Feature | Details |
|---|---|
| 🎨 **Theme & Accent Customization** | Complete UI theming support: Light, Dark, and Midnight (OLED) themes with live interactive preview. Choose between 6 vibrant accent colors (Indigo, Emerald, Sky, Rose, Amber, Purple) dynamically applied across the entire app. |
| 📌 **Customizable Desktop Mini-Widget** | Floating, draggable desktop overlay with live screen time, active app, continuous session timer, profile XP, and mouse metrics. Features click-through mode, topmost toggle, opacity slider, and one-click compact collapse. |
| 🏆 **Gamification, Levels & Achievements** | Earn XP for active time (10 XP/min), unlock levels from Novice to Grandmaster, and earn 12 tiered badges for milestones, consistency, records, and mouse activity. |
| 🌙 **Sleep & Daily Rhythm Analysis** | Synchronized with Windows Event Log system power events (boot, sleep, hibernate, wake) and circular average calculations for exact first wake up, bedtime, and night rest duration. |
| 🖱 **Mouse Activity (Clicks & Distance)** | High-frequency background tracking of mouse clicks and cursor distance traveled in meters/kilometers with jump rejection and formatted counters. |
| 🎮 **Gaming Mode & Detection** | Automatic recognition of games and launchers (Steam, Epic Games, Riot, etc.) with dedicated process filtering. |
| 🔍 **Browser Tabs & Window Titles** | Deep tracking for active browser tabs (Chrome, Edge, Opera, etc.) and editor projects (VS Code, Visual Studio, JetBrains, etc.). Expand any app to see exact per-tab time spent. |
| 📅 **Activity Heatmap** | GitHub-style 20-week contribution grid displaying your daily active hours, streaks, best productive days, and consistency metrics. |
| 📊 **Trends & Period Comparisons** | Compare your screen time against the previous period (*e.g., Today vs. Yesterday, This Week vs. Last Week, This Month vs. Last Month*) with exact diffs and percentages. |
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
- **Fluent UI**: [WPF UI](https://wpfui.lepo.co/) 3.1
- **Charts**: [LiveChartsCore](https://livecharts.dev/) 2.0.5 (SkiaSharp)
- **Database**: SQLite via [Microsoft.Data.Sqlite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/) 10.0 + [Dapper](https://github.com/DapperLib/Dapper) ORM
- **Tray Icon**: [Hardcodet.NotifyIcon.Wpf](https://github.com/hardcodet/wpf-notifyicon) 1.1
- **Exporting**: [CsvHelper](https://joshclose.github.io/CsvHelper/) 33
- **Architecture**: MVVM pattern (no heavyweight frameworks, lightweight reactive properties)

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

### Build with Visual Studio

1. Open `WinTime.sln` in **Visual Studio 2022** (v17.10+) or JetBrains Rider.
2. Select configuration **Release | x64** (or **Debug | x64**).
3. Set `WinTime` as the Startup Project.
4. Press **F5** to run.

### Publish Self-Contained Executable

To generate a standalone executable that runs without requiring a pre-installed .NET Runtime:

```bash
dotnet publish WinTime.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### First Launch

When opening WinTime for the first time, a setup dialog guides you to pick a folder for storing your SQLite `.db` file (default: `%LOCALAPPDATA%\WinTime`). After selection, tracking begins automatically and the app docks to your Windows system tray.

---

## Project Structure

```
WinTime/
├── Core/               # ActivityTracker, Native Win32 APIs, StartupManager, InMemoryBuffer
├── Data/               # DatabaseService, ActivityRepository, ApplicationRepository, UptimeRepository
├── Models/             # AppStatItem, WindowTitleStatItem, ProcessUptimeItem, AchievementItem
├── Services/           # SettingsService, ExportService, IconService
├── ViewModels/         # Dashboard, Processes, Applications, Profile, DesktopWidget, Settings
├── Views/              # Dashboard, Processes, Applications, Profile, DesktopWidgetWindow/Settings
├── Converters/         # SecondsToTimeConverter, BooleanToVisibilityConverter
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

## License

This project is licensed under the [MIT License](LICENSE).
