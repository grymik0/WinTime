using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using WinTime.Core;
using WinTime.Models;
using WinTime.ViewModels;

namespace WinTime.Views;

public partial class GoalsView : UserControl
{
    public GoalsView()
    {
        InitializeComponent();
    }

    private void DialogOverlay_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (DialogOverlay.IsVisible)
        {
            var duration = new Duration(TimeSpan.FromMilliseconds(220));
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            DialogOverlay.BeginAnimation(OpacityProperty,
                new DoubleAnimation(0, 1, duration) { EasingFunction = ease });

            AnimationHelper.PlayZoomFadeIn(DialogCard, fromScale: 0.92, fromY: 14, durationMs: 240);
        }
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

