using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace WinTime;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = AppServices.MainWindowVm;
        AppServices.MainWindowVm.PropertyChanged += OnVmPropertyChanged;

        try
        {
            var sri = Application.GetResourceStream(
                new Uri("pack://application:,,,/Assets/icon.ico"));
            if (sri?.Stream is not null)
                Icon = BitmapFrame.Create(sri.Stream,
                    BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        }
        catch { }
    }

    // Navigation animation

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModels.MainWindowViewModel.CurrentView))
            PlayTransitionAnimation();
    }

    /// <summary>
    /// Smooth tab transition: fade-in and slide-up.
    /// </summary>
    private void PlayTransitionAnimation()
    {
        var transform = (TranslateTransform)ContentArea.RenderTransform;

        ContentArea.Opacity = 0;
        transform.Y = 12;

        var ease     = new CubicEase { EasingMode = EasingMode.EaseOut };
        Duration duration = new Duration(TimeSpan.FromMilliseconds(200));

        ContentArea.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(0, 1, duration) { EasingFunction = ease });

        transform.BeginAnimation(
            TranslateTransform.YProperty,
            new DoubleAnimation(12, 0, duration) { EasingFunction = ease });
    }

    /// <summary>
    /// Closing the main window hides it to the system tray unless application exit was requested.
    /// </summary>
    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (Application.Current is App { IsExiting: false })
        {
            e.Cancel = true;
            Hide();
        }
    }

    // Window caption control handlers

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleWindowState();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    public void ToggleWindowState()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            MaximizeIcon.Data = Geometry.Parse("M 3,5 H 9 V 11 H 3 Z M 5,5 V 3 H 11 V 9 H 9");
            MaximizeBtn.ToolTip = Application.Current?.TryFindResource("Window_Restore") as string ?? "Восстановить";
        }
        else
        {
            MaximizeIcon.Data = Geometry.Parse("M 3,3 H 11 V 11 H 3 Z");
            MaximizeBtn.ToolTip = Application.Current?.TryFindResource("Window_Maximize") as string ?? "Развернуть";
        }
    }

    protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == System.Windows.Input.Key.F11)
        {
            ToggleWindowState();
            e.Handled = true;
        }
    }
}
