using WinTime.Models;

namespace WinTime.Core;

/// <summary>
/// Накапливает текущую активную сессию в памяти (RAM).
/// Метод Flush() сбрасывает завершённые сессии для записи в SQLite.
/// Потокобезопасен через lock.
/// </summary>
public sealed class InMemoryBuffer
{
    private readonly object _lock = new();
    private ActiveEntry? _current;
    private readonly List<ActivitySession> _completed = [];

    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Вызывается каждую секунду из ActivityTracker.
    /// Если контекст (appId, заголовок, isIdle) совпадает — увеличивает счётчик.
    /// Если изменился — завершает старую сессию и открывает новую.
    /// </summary>
    public void Update(int appId, string windowTitle, bool isIdle, DateTime now)
    {
        lock (_lock)
        {
            if (_current is not null)
            {
                bool same = _current.AppId       == appId
                         && _current.WindowTitle  == windowTitle
                         && _current.IsIdle       == isIdle;

                if (same)
                {
                    _current.DurationSeconds++;
                    return;
                }

                // контекст изменился — завершаем старую сессию
                if (_current.DurationSeconds > 0)
                    _completed.Add(_current.ToSession());
            }

            _current = new ActiveEntry(appId, windowTitle, isIdle, now);
        }
    }

    /// <summary>
    /// Принудительно завершает текущую сессию (при смене окна или выходе).
    /// </summary>
    public void EndCurrentSession()
    {
        lock (_lock)
        {
            if (_current is { DurationSeconds: > 0 })
            {
                _completed.Add(_current.ToSession());
                _current = null;
            }
        }
    }

    /// <summary>
    /// Возвращает все накопленные завершённые сессии и очищает буфер.
    /// Текущая незавершённая сессия переоткрывается с тем же контекстом.
    /// </summary>
    public List<ActivitySession> Flush()
    {
        lock (_lock)
        {
            // Снапшот текущей сессии — добавляем в список, но сразу переоткрываем
            if (_current is { DurationSeconds: > 0 })
            {
                _completed.Add(_current.ToSession());
                // Сбрасываем счётчик, чтобы не считать повторно
                _current = new ActiveEntry(
                    _current.AppId, _current.WindowTitle, _current.IsIdle, DateTime.Now);
            }

            var result = new List<ActivitySession>(_completed);
            _completed.Clear();
            return result;
        }
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private sealed class ActiveEntry(int appId, string windowTitle, bool isIdle, DateTime startTime)
    {
        public int    AppId           { get; } = appId;
        public string WindowTitle     { get; } = windowTitle;
        public bool   IsIdle          { get; } = isIdle;
        public DateTime StartTime     { get; } = startTime;
        public int    DurationSeconds { get; set; }

        public ActivitySession ToSession() => new()
        {
            AppId           = AppId,
            WindowTitle     = WindowTitle,
            StartTime       = StartTime,
            DurationSeconds = DurationSeconds,
            IsIdle          = IsIdle
        };
    }
}
