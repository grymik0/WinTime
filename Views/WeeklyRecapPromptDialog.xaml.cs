using System.Windows;
using System.Windows.Input;

namespace WinTime.Views;

public partial class WeeklyRecapPromptDialog : Window
{
    public bool UserWantsToViewRecap { get; private set; }

    public WeeklyRecapPromptDialog()
    {
        InitializeComponent();
        Core.AnimationHelper.AttachWindowEntrance(this);
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void ViewRecap_Click(object sender, RoutedEventArgs e)
    {
        UserWantsToViewRecap = true;
        DialogResult = true;
        Close();
    }

    private void Dismiss_Click(object sender, RoutedEventArgs e)
    {
        UserWantsToViewRecap = false;
        DialogResult = false;
        Close();
    }
}

