using System.ComponentModel;
using System.Windows;

namespace WinTime;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = AppServices.MainWindowVm;
    }

    /// <summary>
    /// Закрытие окна прячет его в трей вместо завершения приложения.
    /// Для реального выхода — пункт «Выход» в контекстном меню трея.
    /// </summary>
    private void Window_Closing(object sender, CancelEventArgs e)
    {
        // Если это не намеренный выход через трей — просто скрываем
        if (Application.Current is App { IsExiting: false })
        {
            e.Cancel = true;
            Hide();
        }
    }
}