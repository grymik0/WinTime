using WinTime.Models;

namespace WinTime.Core;

/// <summary>
/// Thread-safe in-memory buffer accumulating the active window duration before SQLite batched flush.
/// </summary>
public sealed class InMemoryBuffer
{
    private readonly object _lock = new();
    private ActiveEntry? _current;
    private readonly List<ActivitySession> _completed = [];

    /// <summary>
    /// Updates current session duration or rolls over if context changed.
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

                if (_current.DurationSeconds > 0)
                    _completed.Add(_current.ToSession());
            }

            _current = new ActiveEntry(appId, windowTitle, isIdle, now);
        }
    }

    /// <summary>
    /// Forces completion of the active session on window switch or tracker pause.
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
    /// Returns accumulated sessions and resets buffer counters.
    /// </summary>
    public List<ActivitySession> Flush()
    {
        lock (_lock)
        {
            if (_current is { DurationSeconds: > 0 })
            {
                _completed.Add(_current.ToSession());
                _current = new ActiveEntry(
                    _current.AppId, _current.WindowTitle, _current.IsIdle, DateTime.Now);
            }

            var result = new List<ActivitySession>(_completed);
            _completed.Clear();
            return result;
        }
    }

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

