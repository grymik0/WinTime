using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WinTime.Models;

namespace WinTime.Views;

public partial class ProjectEditDialog : Window
{
    public string ProjectName { get; private set; } = string.Empty;
    public string ProjectDesc { get; private set; } = string.Empty;
    public string SelectedColor { get; private set; } = "#6366F1";
    public string SelectedIcon { get; private set; } = "📁";

    private sealed class ColorOption
    {
        public string Hex { get; init; } = string.Empty;
        public SolidColorBrush ColorBrush => new((Color)ColorConverter.ConvertFromString(Hex));
        public Visibility IsSelectedVis { get; set; } = Visibility.Collapsed;
    }

    private sealed class IconOption
    {
        public string Glyph { get; init; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    private readonly List<ColorOption> _colors =
    [
        new() { Hex = "#6366F1" }, // Indigo
        new() { Hex = "#8B5CF6" }, // Purple
        new() { Hex = "#EC4899" }, // Pink
        new() { Hex = "#10B981" }, // Emerald
        new() { Hex = "#F59E0B" }, // Amber
        new() { Hex = "#06B6D4" }, // Cyan
        new() { Hex = "#3B82F6" }, // Blue
        new() { Hex = "#EF4444" }  // Red
    ];

    private readonly string[] _iconGlyphs =
    [
        "📁", "💼", "💻", "🎓", "🎨", "🎮", "🚀", "📚", "✍️", "🛠️", "🔬", "🎵", "💰", "🏠"
    ];

    public ProjectEditDialog(Project? existing = null)
    {
        InitializeComponent();

        if (existing is not null)
        {
            DialogTitleText.Text = "✏️  Редактировать проект";
            NameTextBox.Text = existing.Name;
            DescTextBox.Text = existing.Description;
            SelectedColor = existing.ColorHex;
            SelectedIcon = existing.Icon;
        }

        RefreshColorsUI();
        RefreshIconsUI();
    }

    private void RefreshColorsUI()
    {
        foreach (var c in _colors)
        {
            c.IsSelectedVis = string.Equals(c.Hex, SelectedColor, StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible : Visibility.Collapsed;
        }
        ColorsItemsControl.ItemsSource = null;
        ColorsItemsControl.ItemsSource = _colors;
    }

    private void RefreshIconsUI()
    {
        var iconOptions = _iconGlyphs.Select(g => new IconOption
        {
            Glyph = g,
            IsSelected = g == SelectedIcon
        }).ToList();

        IconsItemsControl.ItemsSource = null;
        IconsItemsControl.ItemsSource = iconOptions;
    }

    private void Color_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string hex })
        {
            SelectedColor = hex;
            RefreshColorsUI();
        }
    }

    private void Icon_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string glyph })
        {
            SelectedIcon = glyph;
            RefreshIconsUI();
        }
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
        string name = NameTextBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Введите название проекта.", "WinTime", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ProjectName = name;
        ProjectDesc = DescTextBox.Text?.Trim() ?? string.Empty;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

