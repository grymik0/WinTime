using System.Globalization;
using System.Windows.Data;

namespace WinTime.Converters;

/// <summary>
/// Конвертирует long/int секунды в читаемую строку «1ч 23м» / «45м 10с» / «30с».
/// Используется в XAML через {Binding ..., Converter={x:Static conv:SecondsToTimeConverter.Instance}}.
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
            return $"{(int)ts.TotalHours}ч {ts.Minutes:D2}м";
        if (ts.TotalMinutes >= 1)
            return $"{ts.Minutes}м {ts.Seconds:D2}с";
        return $"{ts.Seconds}с";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

