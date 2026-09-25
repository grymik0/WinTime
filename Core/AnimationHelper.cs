using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WinTime.Core;

/// <summary>
/// Provides Fluent-style Zoom & Fade animations for views, dialogs, and windows.
/// </summary>
public static class AnimationHelper
{
    private static readonly IEasingFunction DefaultEase = new CubicEase { EasingMode = EasingMode.EaseOut };

    /// <summary>
    /// Plays a smooth Fluent Zoom & Fade entrance animation on the specified element.
    /// </summary>
    /// <param name="element">The UI element to animate.</param>
    /// <param name="fromScale">Initial scale (e.g. 0.96 for subtle zoom, 0.92 for dialogs).</param>
    /// <param name="fromY">Initial vertical offset for subtle upward motion.</param>
    /// <param name="durationMs">Animation duration in milliseconds.</param>
    public static void PlayZoomFadeIn(
        FrameworkElement element,
        double fromScale = 0.96,
        double fromY = 8,
        int durationMs = 240)
    {
        if (element == null) return;

        element.RenderTransformOrigin = new Point(0.5, 0.4);

        ScaleTransform scaleTransform;
        TranslateTransform translateTransform;

        if (element.RenderTransform is TransformGroup group &&
            group.Children.Count >= 2 &&
            group.Children[0] is ScaleTransform st &&
            group.Children[1] is TranslateTransform tt)
        {
            scaleTransform = st;
            translateTransform = tt;
        }
        else
        {
            scaleTransform = new ScaleTransform(1, 1);
            translateTransform = new TranslateTransform(0, 0);
            var tg = new TransformGroup();
            tg.Children.Add(scaleTransform);
            tg.Children.Add(translateTransform);
            element.RenderTransform = tg;
        }

        var duration = new Duration(TimeSpan.FromMilliseconds(durationMs));

        // Fade in opacity
        element.Opacity = 0;
        var opacityAnim = new DoubleAnimation(0, 1, duration) { EasingFunction = DefaultEase };
        element.BeginAnimation(UIElement.OpacityProperty, opacityAnim);

        // Zoom in scale
        scaleTransform.ScaleX = fromScale;
        scaleTransform.ScaleY = fromScale;
        var scaleAnim = new DoubleAnimation(fromScale, 1.0, duration) { EasingFunction = DefaultEase };
        scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
        scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

        // Settle vertically
        if (fromY != 0)
        {
            translateTransform.Y = fromY;
            var translateAnim = new DoubleAnimation(fromY, 0, duration) { EasingFunction = DefaultEase };
            translateTransform.BeginAnimation(TranslateTransform.YProperty, translateAnim);
        }
    }

    /// <summary>
    /// Attaches a Fluent Zoom & Fade entrance animation to a Window's root content on load.
    /// </summary>
    public static void AttachWindowEntrance(
        Window window,
        double fromScale = 0.93,
        double fromY = 12,
        int durationMs = 240)
    {
        window.Loaded += (s, e) =>
        {
            if (window.Content is FrameworkElement root)
            {
                PlayZoomFadeIn(root, fromScale, fromY, durationMs);
            }
        };
    }
}

