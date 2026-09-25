using System.Windows;
using System.Windows.Input;
using WinTime.Models;

namespace WinTime.Views;

public partial class ProjectRuleDialog : Window
{
    public int? SelectedAppId { get; private set; }
    public string Keyword { get; private set; } = string.Empty;

    public sealed class AppItemOption
    {
        public int? Id { get; init; }
        public string DisplayName { get; init; } = string.Empty;
    }

    public ProjectRuleDialog(IReadOnlyList<AppModel> availableApps)
    {
        InitializeComponent();
        Core.AnimationHelper.AttachWindowEntrance(this);

        var res = Application.Current?.Resources;
        string anyAppText = (string)(res?["RuleDlg_AnyApp"] ?? "— Любое приложение (по ключевому слову) —");

        var list = new List<AppItemOption>
        {
            new() { Id = null, DisplayName = anyAppText }
        };

        foreach (var app in availableApps.OrderBy(a => a.DisplayName))
        {
            list.Add(new AppItemOption
            {
                Id = app.Id,
                DisplayName = !string.IsNullOrWhiteSpace(app.DisplayName) ? app.DisplayName : app.ProcessName
            });
        }

        AppsComboBox.ItemsSource = list;
        AppsComboBox.SelectedIndex = list.Count > 1 ? 1 : 0;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var selected = AppsComboBox.SelectedItem as AppItemOption;
        string kw = KeywordTextBox.Text?.Trim() ?? string.Empty;

        if (!selected?.Id.HasValue == true && string.IsNullOrWhiteSpace(kw))
        {
            var res = Application.Current?.Resources;
            string msg = (string)(res?["RuleDlg_AppOrKeywordRequired"] ?? "Выберите приложение или укажите ключевое слово для правила.");
            MessageBox.Show(msg, "WinTime", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SelectedAppId = selected?.Id;
        Keyword = kw;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
