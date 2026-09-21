using System.Globalization;
using System.Windows.Data;

namespace WinTime.Converters;

/// <summary>
/// Converts seconds (long/int) into human-readable duration strings.
/// </summary>
[ValueConversion(typeof(long), typeof(string))]
public sealed class SecondsToTimeConverter : IValueConverter
{
    public static readonly SecondsToTimeConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        long seconds = value switch
        {
            long l  => l,
            int  i  => i,
            _       => 0L
        };

        var ts = TimeSpan.FromSeconds(seconds);
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes:D2}m";
        if (ts.TotalMinutes >= 1)
            return $"{ts.Minutes}m {ts.Seconds:D2}s";
        return $"{ts.Seconds}s";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

