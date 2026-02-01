using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AVCNDB.WPF.Controls;

/// <summary>
/// Contrôle pour la saisie de valeurs décimales (prix, quantités)
/// </summary>
public class DecimalTextBox : TextBox
{
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(decimal?),
            typeof(DecimalTextBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public static readonly DependencyProperty DecimalPlacesProperty =
        DependencyProperty.Register(
            nameof(DecimalPlaces),
            typeof(int),
            typeof(DecimalTextBox),
            new PropertyMetadata(2));

    public static readonly DependencyProperty MinValueProperty =
        DependencyProperty.Register(
            nameof(MinValue),
            typeof(decimal?),
            typeof(DecimalTextBox),
            new PropertyMetadata(null));

    public static readonly DependencyProperty MaxValueProperty =
        DependencyProperty.Register(
            nameof(MaxValue),
            typeof(decimal?),
            typeof(DecimalTextBox),
            new PropertyMetadata(null));

    public static readonly DependencyProperty SuffixProperty =
        DependencyProperty.Register(
            nameof(Suffix),
            typeof(string),
            typeof(DecimalTextBox),
            new PropertyMetadata(string.Empty));

    public decimal? Value
    {
        get => (decimal?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int DecimalPlaces
    {
        get => (int)GetValue(DecimalPlacesProperty);
        set => SetValue(DecimalPlacesProperty, value);
    }

    public decimal? MinValue
    {
        get => (decimal?)GetValue(MinValueProperty);
        set => SetValue(MinValueProperty, value);
    }

    public decimal? MaxValue
    {
        get => (decimal?)GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    public string Suffix
    {
        get => (string)GetValue(SuffixProperty);
        set => SetValue(SuffixProperty, value);
    }

    private bool _isUpdating;

    public DecimalTextBox()
    {
        HorizontalContentAlignment = HorizontalAlignment.Right;
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DecimalTextBox textBox && !textBox._isUpdating)
        {
            textBox.UpdateText();
        }
    }

    private void UpdateText()
    {
        _isUpdating = true;
        Text = Value?.ToString($"N{DecimalPlaces}") ?? string.Empty;
        _isUpdating = false;
    }

    protected override void OnTextChanged(TextChangedEventArgs e)
    {
        base.OnTextChanged(e);

        if (_isUpdating) return;

        _isUpdating = true;
        
        if (decimal.TryParse(Text.Replace(" ", ""), out var value))
        {
            // Appliquer les limites
            if (MinValue.HasValue && value < MinValue.Value)
                value = MinValue.Value;
            if (MaxValue.HasValue && value > MaxValue.Value)
                value = MaxValue.Value;

            Value = Math.Round(value, DecimalPlaces);
        }
        else if (string.IsNullOrWhiteSpace(Text))
        {
            Value = null;
        }

        _isUpdating = false;
    }

    protected override void OnPreviewTextInput(TextCompositionEventArgs e)
    {
        // Autoriser uniquement les chiffres, le point et la virgule
        var allowed = "0123456789.,";
        e.Handled = !e.Text.All(c => allowed.Contains(c));
        
        // Empêcher plusieurs séparateurs
        if (!e.Handled && (e.Text == "." || e.Text == ","))
        {
            e.Handled = Text.Contains('.') || Text.Contains(',');
        }

        base.OnPreviewTextInput(e);
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        UpdateText();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        // Augmenter/diminuer avec les flèches
        if (e.Key == Key.Up)
        {
            var increment = (decimal)Math.Pow(10, -DecimalPlaces);
            Value = (Value ?? 0) + increment;
            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            var increment = (decimal)Math.Pow(10, -DecimalPlaces);
            Value = Math.Max((Value ?? 0) - increment, MinValue ?? 0);
            e.Handled = true;
        }

        base.OnPreviewKeyDown(e);
    }
}
