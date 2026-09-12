using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinTime.ViewModels;

/// <summary>
/// Базовый класс всех ViewModel. Реализует INotifyPropertyChanged.
/// </summary>
public abstract class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>
    /// Устанавливает поле и вызывает OnPropertyChanged, если значение изменилось.
    /// Возвращает true если значение изменилось.
    /// </summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}

