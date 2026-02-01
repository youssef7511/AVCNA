using System.Windows;
using System.Windows.Controls;

namespace AVCNDB.WPF.Controls;

/// <summary>
/// Contrôle de carte de médicament avec affichage stylisé
/// </summary>
public class MedicCard : Control
{
    public static readonly DependencyProperty MedicNameProperty =
        DependencyProperty.Register(nameof(MedicName), typeof(string), typeof(MedicCard));

    public static readonly DependencyProperty DciNameProperty =
        DependencyProperty.Register(nameof(DciName), typeof(string), typeof(MedicCard));

    public static readonly DependencyProperty DosageProperty =
        DependencyProperty.Register(nameof(Dosage), typeof(string), typeof(MedicCard));

    public static readonly DependencyProperty FormeProperty =
        DependencyProperty.Register(nameof(Forme), typeof(string), typeof(MedicCard));

    public static readonly DependencyProperty LaboProperty =
        DependencyProperty.Register(nameof(Labo), typeof(string), typeof(MedicCard));

    public static readonly DependencyProperty PriceProperty =
        DependencyProperty.Register(nameof(Price), typeof(decimal?), typeof(MedicCard));

    public static readonly DependencyProperty IsPediatricProperty =
        DependencyProperty.Register(nameof(IsPediatric), typeof(bool), typeof(MedicCard));

    public static readonly DependencyProperty IsGenericProperty =
        DependencyProperty.Register(nameof(IsGeneric), typeof(bool), typeof(MedicCard));

    public static readonly DependencyProperty IsControlledProperty =
        DependencyProperty.Register(nameof(IsControlled), typeof(bool), typeof(MedicCard));

    public static readonly DependencyProperty TableauProperty =
        DependencyProperty.Register(nameof(Tableau), typeof(string), typeof(MedicCard));

    public string MedicName
    {
        get => (string)GetValue(MedicNameProperty);
        set => SetValue(MedicNameProperty, value);
    }

    public string DciName
    {
        get => (string)GetValue(DciNameProperty);
        set => SetValue(DciNameProperty, value);
    }

    public string Dosage
    {
        get => (string)GetValue(DosageProperty);
        set => SetValue(DosageProperty, value);
    }

    public string Forme
    {
        get => (string)GetValue(FormeProperty);
        set => SetValue(FormeProperty, value);
    }

    public string Labo
    {
        get => (string)GetValue(LaboProperty);
        set => SetValue(LaboProperty, value);
    }

    public decimal? Price
    {
        get => (decimal?)GetValue(PriceProperty);
        set => SetValue(PriceProperty, value);
    }

    public bool IsPediatric
    {
        get => (bool)GetValue(IsPediatricProperty);
        set => SetValue(IsPediatricProperty, value);
    }

    public bool IsGeneric
    {
        get => (bool)GetValue(IsGenericProperty);
        set => SetValue(IsGenericProperty, value);
    }

    public bool IsControlled
    {
        get => (bool)GetValue(IsControlledProperty);
        set => SetValue(IsControlledProperty, value);
    }

    public string Tableau
    {
        get => (string)GetValue(TableauProperty);
        set => SetValue(TableauProperty, value);
    }

    static MedicCard()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(MedicCard),
            new FrameworkPropertyMetadata(typeof(MedicCard)));
    }
}
