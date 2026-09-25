using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using WinTime.Services;

namespace WinTime.Views;

/// <summary>
/// First-run configuration dialog for selecting database location and interface language.
/// </summary>
public partial class FirstRunDialog : Window
{
    private readonly SettingsService _settings;
    private readonly LocalizationService _localization;

    public string? SelectedDbPath { get; private set; }

    public FirstRunDialog(SettingsService settings, LocalizationService localization)
    {
        _settings = settings;
        _localization = localization;
        InitializeComponent();
        Core.AnimationHelper.AttachWindowEntrance(this);
        UpdateLanguageButtonStyles();
    }

    private void BtnLangRu_Click(object sender, RoutedEventArgs e)
    {
        _localization.ApplyLanguage(AppLanguage.Ru, saveSettings: true);
        UpdateLanguageButtonStyles();
    }

    private void BtnLangEn_Click(object sender, RoutedEventArgs e)
    {
        _localization.ApplyLanguage(AppLanguage.En, saveSettings: true);
        UpdateLanguageButtonStyles();
    }

    private void UpdateLanguageButtonStyles()
    {
        bool isRu = _localization.CurrentLanguage == AppLanguage.Ru;

        BtnLangRu.Background = isRu ? (Brush)Application.Current.Resources["AccentBrush"] : new SolidColorBrush(Color.FromRgb(45, 45, 60));
        BtnLangRu.Foreground = Brushes.White;
        BtnLangRu.BorderBrush = isRu ? (Brush)Application.Current.Resources["AccentBrush"] : new SolidColorBrush(Color.FromRgb(75, 85, 99));

        BtnLangEn.Background = !isRu ? (Brush)Application.Current.Resources["AccentBrush"] : new SolidColorBrush(Color.FromRgb(45, 45, 60));
        BtnLangEn.Foreground = Brushes.White;
        BtnLangEn.BorderBrush = !isRu ? (Brush)Application.Current.Resources["AccentBrush"] : new SolidColorBrush(Color.FromRgb(75, 85, 99));
    }

    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Title            = _localization.GetString("Setup_Title"),
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
        var emptyPathText = _localization.GetString("Setup_PathEmpty");
        if (string.IsNullOrWhiteSpace(TxtPath.Text) || TxtPath.Text == emptyPathText)
        {
            var warningMsg = _localization.CurrentLanguage == AppLanguage.Ru
                ? "Пожалуйста, выберите путь к файлу базы данных."
                : "Please select a database file path.";
            MessageBox.Show(warningMsg, "WinTime", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SelectedDbPath = TxtPath.Text;
        DialogResult   = true;
    }
}

