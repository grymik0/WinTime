using System.Windows;
using System.Windows.Input;
using WinTime.Services;
using WinTime.ViewModels;

namespace WinTime.Views;

public partial class DesktopWidgetWindow : Window
{
    private readonly SettingsService _settings;

    public DesktopWidgetWindow(DesktopWidgetViewModel vm, SettingsService settings)
    {
        InitializeComponent();
        DataContext = vm;
        _settings   = settings;

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Opacity = _settings.WidgetOpacity;
        Topmost = _settings.WidgetTopmost;
        UpdateClickThrough(_settings.WidgetClickThrough);

        // Восстанавливаем позицию если сохранена, иначе в правый верхний угол
        if (_settings.WidgetLeft >= 0 && _settings.WidgetTop >= 0)
        {
            Left = _settings.WidgetLeft;
            Top  = _settings.WidgetTop;
        }
        else
        {
            ResetPosition();
        }
    }

    public void ResetPosition()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 24;
        Top  = workArea.Top + 36;
        _settings.WidgetLeft = Left;
        _settings.WidgetTop  = Top;
    }

    public void UpdateOpacity()
    {
        Opacity = _settings.WidgetOpacity;
    }

    public void UpdateTopmost(bool topmost)
    {
        Topmost = topmost;
    }

    public void UpdateClickThrough(bool enable)
    {
        try
        {
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            var hwnd = helper.Handle;
            if (hwnd == IntPtr.Zero) return;

            long exStyle = Core.NativeMethods.GetWindowLongPtr(hwnd, Core.NativeMethods.GWL_EXSTYLE);
            if (enable)
            {
                exStyle |= Core.NativeMethods.WS_EX_TRANSPARENT;
            }
            else
            {
                exStyle &= ~Core.NativeMethods.WS_EX_TRANSPARENT;
            }
            Core.NativeMethods.SetWindowLongPtr(hwnd, Core.NativeMethods.GWL_EXSTYLE, new IntPtr(exStyle));
        }
        catch { }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void Window_LocationChanged(object? sender, EventArgs e)
    {
        if (IsLoaded && Left >= 0 && Top >= 0)
        {
            _settings.WidgetLeft = Left;
            _settings.WidgetTop  = Top;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _settings.ShowWidget = false;
        Hide();
    }
}
