using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace AVCNDB.WPF.Behaviors;

/// <summary>
/// Attached behavior that animates a TextBlock's displayed integer value
/// from its previous state to <see cref="ValueProperty"/> using an
/// Int32Animation with a cubic-out easing. Used by the dashboard hero
/// numeral so the page-load reveal counts up rather than snapping in.
/// </summary>
public static class AnimatedInt
{
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.RegisterAttached(
            "Value", typeof(int), typeof(AnimatedInt),
            new PropertyMetadata(0, OnValueChanged));

    public static int GetValue(DependencyObject o) => (int)o.GetValue(ValueProperty);
    public static void SetValue(DependencyObject o, int v) => o.SetValue(ValueProperty, v);

    public static readonly DependencyProperty FormatProperty =
        DependencyProperty.RegisterAttached(
            "Format", typeof(string), typeof(AnimatedInt),
            new PropertyMetadata("N0"));

    public static string GetFormat(DependencyObject o) => (string)o.GetValue(FormatProperty);
    public static void SetFormat(DependencyObject o, string v) => o.SetValue(FormatProperty, v);

    public static readonly DependencyProperty DurationMsProperty =
        DependencyProperty.RegisterAttached(
            "DurationMs", typeof(int), typeof(AnimatedInt),
            new PropertyMetadata(900));

    public static int GetDurationMs(DependencyObject o) => (int)o.GetValue(DurationMsProperty);
    public static void SetDurationMs(DependencyObject o, int v) => o.SetValue(DurationMsProperty, v);

    public static readonly DependencyProperty BeginTimeMsProperty =
        DependencyProperty.RegisterAttached(
            "BeginTimeMs", typeof(int), typeof(AnimatedInt),
            new PropertyMetadata(0));

    public static int GetBeginTimeMs(DependencyObject o) => (int)o.GetValue(BeginTimeMsProperty);
    public static void SetBeginTimeMs(DependencyObject o, int v) => o.SetValue(BeginTimeMsProperty, v);

    private static readonly DependencyProperty CurrentProperty =
        DependencyProperty.RegisterAttached(
            "Current", typeof(int), typeof(AnimatedInt),
            new PropertyMetadata(0, OnCurrentChanged));

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock tb) return;

        int newVal = (int)e.NewValue;
        int current = (int)d.GetValue(CurrentProperty);

        var anim = new Int32Animation
        {
            From = current,
            To = newVal,
            Duration = TimeSpan.FromMilliseconds(GetDurationMs(d)),
            BeginTime = TimeSpan.FromMilliseconds(GetBeginTimeMs(d)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        tb.BeginAnimation(CurrentProperty, anim);
    }

    private static void OnCurrentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock tb) return;
        var fmt = GetFormat(d);
        tb.Text = ((int)e.NewValue).ToString(fmt, CultureInfo.GetCultureInfo("fr-FR"));
    }
}
