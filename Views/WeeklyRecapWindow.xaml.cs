using System.Windows;
using System.Windows.Input;
using WinTime.ViewModels;

namespace WinTime.Views;

public partial class WeeklyRecapWindow : Window
{
    public WeeklyRecapWindow(WeeklyRecapViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

