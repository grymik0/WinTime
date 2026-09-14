using System.Windows;
using System.Windows.Controls;
using WinTime.Models;
using WinTime.ViewModels;

namespace WinTime.Views;

public partial class ProcessesView : UserControl
{
    public ProcessesView()
    {
        InitializeComponent();
    }

    private void AppsGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        if (e.Row.Item is ProcessUptimeItem item)
        {
            e.Row.DetailsVisibility = item.IsExpanded ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void ToggleExpand_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ProcessUptimeItem item)
        {
            var row = DataGridRow.GetRowContainingElement(fe);
            item.IsExpanded = !item.IsExpanded;
            if (row != null)
            {
                row.DetailsVisibility = item.IsExpanded ? Visibility.Visible : Visibility.Collapsed;
            }

            if (item.IsExpanded && DataContext is ProcessesViewModel vm)
            {
                _ = vm.LoadTitlesForItemAsync(item);
            }
        }
    }
}
