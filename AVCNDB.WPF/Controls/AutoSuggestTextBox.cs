using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AVCNDB.WPF.Controls;

/// <summary>
/// Zone de texte avec suggestion automatique
/// </summary>
public class AutoSuggestTextBox : TextBox
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable<object>),
            typeof(AutoSuggestTextBox),
            new PropertyMetadata(null));

    public static readonly DependencyProperty DisplayMemberPathProperty =
        DependencyProperty.Register(
            nameof(DisplayMemberPath),
            typeof(string),
            typeof(AutoSuggestTextBox),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(
            nameof(SelectedItem),
            typeof(object),
            typeof(AutoSuggestTextBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty MinCharactersProperty =
        DependencyProperty.Register(
            nameof(MinCharacters),
            typeof(int),
            typeof(AutoSuggestTextBox),
            new PropertyMetadata(2));

    private Popup? _popup;
    private ListBox? _listBox;

    public IEnumerable<object> ItemsSource
    {
        get => (IEnumerable<object>)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public string DisplayMemberPath
    {
        get => (string)GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    public object SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public int MinCharacters
    {
        get => (int)GetValue(MinCharactersProperty);
        set => SetValue(MinCharactersProperty, value);
    }

    static AutoSuggestTextBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(AutoSuggestTextBox),
            new FrameworkPropertyMetadata(typeof(AutoSuggestTextBox)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _popup = GetTemplateChild("PART_Popup") as Popup;
        _listBox = GetTemplateChild("PART_ListBox") as ListBox;

        if (_listBox != null)
        {
            _listBox.SelectionChanged += OnListBoxSelectionChanged;
            _listBox.PreviewMouseDown += OnListBoxPreviewMouseDown;
        }
    }

    protected override void OnTextChanged(TextChangedEventArgs e)
    {
        base.OnTextChanged(e);

        if (Text.Length >= MinCharacters && ItemsSource != null)
        {
            var filtered = FilterItems(Text);
            if (_listBox != null)
            {
                _listBox.ItemsSource = filtered;
            }

            if (_popup != null)
            {
                _popup.IsOpen = filtered.Any();
            }
        }
        else if (_popup != null)
        {
            _popup.IsOpen = false;
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (_popup?.IsOpen == true && _listBox != null)
        {
            switch (e.Key)
            {
                case Key.Down:
                    if (_listBox.SelectedIndex < _listBox.Items.Count - 1)
                        _listBox.SelectedIndex++;
                    e.Handled = true;
                    break;

                case Key.Up:
                    if (_listBox.SelectedIndex > 0)
                        _listBox.SelectedIndex--;
                    e.Handled = true;
                    break;

                case Key.Enter:
                    SelectCurrentItem();
                    e.Handled = true;
                    break;

                case Key.Escape:
                    _popup.IsOpen = false;
                    e.Handled = true;
                    break;
            }
        }
    }

    private IEnumerable<object> FilterItems(string filter)
    {
        if (ItemsSource == null) return Enumerable.Empty<object>();

        return ItemsSource.Where(item =>
        {
            var value = GetDisplayValue(item);
            return value?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false;
        }).Take(10);
    }

    private string? GetDisplayValue(object item)
    {
        if (string.IsNullOrEmpty(DisplayMemberPath))
            return item?.ToString();

        var property = item?.GetType().GetProperty(DisplayMemberPath);
        return property?.GetValue(item)?.ToString();
    }

    private void OnListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Sélection par clavier
    }

    private void OnListBoxPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        SelectCurrentItem();
    }

    private void SelectCurrentItem()
    {
        if (_listBox?.SelectedItem != null)
        {
            SelectedItem = _listBox.SelectedItem;
            Text = GetDisplayValue(_listBox.SelectedItem) ?? string.Empty;
            CaretIndex = Text.Length;
        }

        if (_popup != null)
        {
            _popup.IsOpen = false;
        }
    }
}

/// <summary>
/// Représente un popup pour l'AutoSuggestTextBox
/// </summary>
public class Popup : System.Windows.Controls.Primitives.Popup
{
}
