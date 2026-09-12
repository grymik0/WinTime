using System.Windows;
using Microsoft.Win32;

namespace WinTime.Views;

/// <summary>
/// Диалог первого запуска — пользователь выбирает путь к файлу БД.
/// </summary>
public partial class FirstRunDialog : Window
{
    /// <summary>Выбранный путь к файлу БД (заполняется при DialogResult = true).</summary>
    public string? SelectedDbPath { get; private set; }

    public FirstRunDialog()
    {
        InitializeComponent();
    }

    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Title            = "Укажите файл базы данных WinTime",
            Filter           = "SQLite Database (*.db)|*.db",
            FileName         = "wintime.db",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            OverwritePrompt  = false
        };

        if (dlg.ShowDialog() == true)
            TxtPath.Text = dlg.FileName;
    }

    private void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtPath.Text))
        {
            MessageBox.Show("Пожалуйста, выберите путь к файлу базы данных.",
                "WinTime", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SelectedDbPath = TxtPath.Text;
        DialogResult   = true;
    }
}
