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
}
