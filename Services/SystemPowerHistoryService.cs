using System.Diagnostics.Eventing.Reader;
using System.Xml.Linq;

namespace WinTime.Services;

/// <summary>
/// Сервис для получения реальной истории включений/пробуждений и выключений/уходов в сон Windows из системного журнала событий (Event Log).
/// </summary>
public static class SystemPowerHistoryService
{
    private static readonly XNamespace Ns = "http://schemas.microsoft.com/win/2004/08/events/event";

    /// <summary>
    /// Возвращает словарь по дням: Date (DateTime.Date) -> (EarliestWakeTime, LatestSleepTime)
    /// за последние <paramref name="days"/> дней.
    /// </summary>
    public static Dictionary<DateTime, (TimeSpan FirstWake, TimeSpan LastSleep)> GetPowerHistory(int days = 30)
    {
        var result = new Dictionary<DateTime, (TimeSpan FirstWake, TimeSpan LastSleep)>();
        var fromDate = DateTime.Today.AddDays(-days);

        try
        {
            var query = new EventLogQuery("System", PathType.LogName,
                "*[System[Provider[@Name='Microsoft-Windows-Power-Troubleshooter'] and EventID=1]]");

            using var reader = new EventLogReader(query);
            EventRecord? record;

            while ((record = reader.ReadEvent()) != null)
            {
                using (record)
                {
                    try
                    {
                        var xmlStr = record.ToXml();
                        var doc = XDocument.Parse(xmlStr);
                        var eventData = doc.Root?.Element(Ns + "EventData");
                        if (eventData == null) continue;

                        string? wakeStr = null;
                        string? sleepStr = null;

                        foreach (var dataElem in eventData.Elements(Ns + "Data"))
                        {
                            var name = dataElem.Attribute("Name")?.Value;
                            if (name == "WakeTime")
                                wakeStr = dataElem.Value;
                            else if (name == "SleepTime")
                                sleepStr = dataElem.Value;
                        }

                        if (!string.IsNullOrEmpty(wakeStr) && DateTime.TryParse(wakeStr, out var wakeUtc))
                        {
                            var wakeLocal = wakeUtc.ToLocalTime();
                            if (wakeLocal.Date >= fromDate)
                            {
                                var date = wakeLocal.Date;
                                var wakeTime = wakeLocal.TimeOfDay;

                                if (result.TryGetValue(date, out var current))
                                {
                                    var earliest = wakeTime < current.FirstWake ? wakeTime : current.FirstWake;
                                    result[date] = (earliest, current.LastSleep);
                                }
                                else
                                {
                                    result[date] = (wakeTime, wakeTime);
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(sleepStr) && DateTime.TryParse(sleepStr, out var sleepUtc))
                        {
                            var sleepLocal = sleepUtc.ToLocalTime();
                            if (sleepLocal.Date >= fromDate)
                            {
                                var date = sleepLocal.Date;
                                var sleepTime = sleepLocal.TimeOfDay;

                                if (result.TryGetValue(date, out var current))
                                {
                                    var latest = sleepTime > current.LastSleep ? sleepTime : current.LastSleep;
                                    result[date] = (current.FirstWake, latest);
                                }
                                else
                                {
                                    result[date] = (sleepTime, sleepTime);
                                }
                            }
                        }
                    }
                    catch
                    {
                      
                    }
                }
            }
        }
        catch
        {
           
        }

        try
        {
            var bootTimeLocal = (DateTime.UtcNow - TimeSpan.FromMilliseconds(Environment.TickCount64)).ToLocalTime();
            if (bootTimeLocal.Date >= fromDate)
            {
                var bDate = bootTimeLocal.Date;
                var bTime = bootTimeLocal.TimeOfDay;

                if (result.TryGetValue(bDate, out var cur))
                {
                    if (bTime < cur.FirstWake)
                    {
                        result[bDate] = (bTime, cur.LastSleep);
                    }
                }
                else
                {
                    result[bDate] = (bTime, bTime);
                }
            }
        }
        catch { }

        return result;
    }
}

