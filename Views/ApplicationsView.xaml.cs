using System.Windows;
using System.Windows.Controls;
using WinTime.Models;
using WinTime.ViewModels;

namespace WinTime.Views;

public partial class ApplicationsView : UserControl
{
    public ApplicationsView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Сохраняем строку при завершении редактирования (Commit).
    /// </summary>
    private void AppsGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit) return;

        if (e.Row.DataContext is AppModel app &&
            DataContext is ApplicationsViewModel vm)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                () => vm.SaveRowCommand.Execute(app));
        }
    }

    /// <summary>
    /// Чекбокс «Не отслеживать» — сохраняем сразу при клике,
    /// не дожидаясь выхода строки из режима редактирования.
    /// </summary>
    private void BlacklistCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox { DataContext: AppModel app } &&
            DataContext is ApplicationsViewModel vm)
        {
            vm.SaveRowCommand.Execute(app);
        }
    }
}

