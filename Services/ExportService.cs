using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using CsvHelper;
using WinTime.Data;
using WinTime.Models;

namespace WinTime.Services;

/// <summary>
/// Экспортирует статистику активности в CSV и JSON.
/// </summary>
public sealed class ExportService
{
    private readonly ActivityRepository _activityRepo;
    private readonly ApplicationRepository _appRepo;

    public ExportService(ActivityRepository activityRepo, ApplicationRepository appRepo)
    {
        _activityRepo = activityRepo;
        _appRepo      = appRepo;
    }

    // CSV

    public async Task ExportToCsvAsync(string filePath)
    {
        // Экспортируем всю статистику за всё время
        var stats = await _activityRepo.GetTopAppsAsync(DateTime.MinValue, DateTime.MaxValue);

        await using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        await using var csv    = new CsvWriter(writer, CultureInfo.InvariantCulture);

        // Заголовки
        csv.WriteField("Приложение");
        csv.WriteField("Процесс");
        csv.WriteField("Часов");
        csv.WriteField("Минут");
        csv.WriteField("Время (форм.)");
        await csv.NextRecordAsync();

        foreach (var s in stats)
        {
            var ts = TimeSpan.FromSeconds(s.TotalSeconds);
            csv.WriteField(s.DisplayName);
            csv.WriteField(s.ProcessName);
            csv.WriteField(Math.Round(ts.TotalHours, 2));
            csv.WriteField((int)ts.TotalMinutes);
            csv.WriteField(s.FormattedTime);
            await csv.NextRecordAsync();
        }
    }

    // JSON

    public async Task ExportToJsonAsync(string filePath)
    {
        var stats = await _activityRepo.GetTopAppsAsync(DateTime.MinValue, DateTime.MaxValue);

        var payload = stats.Select(s => new
        {
            app     = s.DisplayName,
            process = s.ProcessName,
            totalSeconds = s.TotalSeconds,
            formatted    = s.FormattedTime
        });

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
    }
}

