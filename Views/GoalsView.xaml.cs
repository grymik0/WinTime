using System.Windows;
using System.Windows.Controls;
using WinTime.Models;
using WinTime.ViewModels;

namespace WinTime.Views;

public partial class GoalsView : UserControl
{
    public GoalsView()
    {
        InitializeComponent();
    }

    private void OnNotifyChecked(object sender, RoutedEventArgs e)
    {
        if (DataContext is GoalsViewModel vm)
            vm.DialogActionType = LimitActionType.Notify;
    }

    private void OnCloseChecked(object sender, RoutedEventArgs e)
    {
        if (DataContext is GoalsViewModel vm)
            vm.DialogActionType = LimitActionType.CloseProcess;
    }
}

