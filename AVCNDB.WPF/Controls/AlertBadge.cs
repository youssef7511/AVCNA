using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AVCNDB.WPF.Controls;

/// <summary>
/// Badge d'alerte avec différents niveaux de sévérité
/// </summary>
public class AlertBadge : Control
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(AlertBadge));

    public static readonly DependencyProperty CountProperty =
        DependencyProperty.Register(nameof(Count), typeof(int?), typeof(AlertBadge));

    public static readonly DependencyProperty SeverityProperty =
        DependencyProperty.Register(nameof(Severity), typeof(AlertSeverity), typeof(AlertBadge),
            new PropertyMetadata(AlertSeverity.Info, OnSeverityChanged));

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(string), typeof(AlertBadge));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public int? Count
    {
        get => (int?)GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    public AlertSeverity Severity
    {
        get => (AlertSeverity)GetValue(SeverityProperty);
        set => SetValue(SeverityProperty, value);
    }

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    // Propriétés calculées pour le style
    public Brush BackgroundColor => Severity switch
    {
        AlertSeverity.Success => new SolidColorBrush(Color.FromRgb(200, 230, 201)),
        AlertSeverity.Warning => new SolidColorBrush(Color.FromRgb(255, 243, 224)),
        AlertSeverity.Error => new SolidColorBrush(Color.FromRgb(255, 205, 210)),
        AlertSeverity.Info => new SolidColorBrush(Color.FromRgb(187, 222, 251)),
        _ => new SolidColorBrush(Colors.LightGray)
    };

    public Brush ForegroundColor => Severity switch
    {
        AlertSeverity.Success => new SolidColorBrush(Color.FromRgb(27, 94, 32)),
        AlertSeverity.Warning => new SolidColorBrush(Color.FromRgb(230, 81, 0)),
        AlertSeverity.Error => new SolidColorBrush(Color.FromRgb(183, 28, 28)),
        AlertSeverity.Info => new SolidColorBrush(Color.FromRgb(13, 71, 161)),
        _ => new SolidColorBrush(Colors.Black)
    };

    static AlertBadge()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(AlertBadge),
            new FrameworkPropertyMetadata(typeof(AlertBadge)));
    }

    private static void OnSeverityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Déclencher la mise à jour des couleurs
    }
}

/// <summary>
/// Niveaux de sévérité des alertes
/// </summary>
public enum AlertSeverity
{
    Info,
    Success,
    Warning,
    Error
}
