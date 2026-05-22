using System.Collections;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace AVCNDB.WPF.Controls;

/// <summary>
/// ComboBox with real-time autocomplete filtering.
/// Bind to <see cref="FilterableItemsSource"/> instead of ItemsSource.
/// Each instance gets its own view so filtering one doesn't affect others
/// even when they share the same source collection.
/// Clearing the text and leaving the control clears the selection.
/// </summary>
public class FilterableComboBox : ComboBox
{
    // ─── Dependency Properties ──────────────────────────────────────

    public static readonly DependencyProperty FilterableItemsSourceProperty =
        DependencyProperty.Register(
            nameof(FilterableItemsSource),
            typeof(IEnumerable),
            typeof(FilterableComboBox),
            new PropertyMetadata(null, OnFilterableItemsSourceChanged));

    public IEnumerable? FilterableItemsSource
    {
        get => (IEnumerable?)GetValue(FilterableItemsSourceProperty);
        set => SetValue(FilterableItemsSourceProperty, value);
    }

    // ─── Private state ──────────────────────────────────────────────

    private TextBox? _editBox;
    private ListCollectionView? _view;
    private bool _isSelecting;
    private bool _isResettingFilter;
    private bool _isLoadingItems;
    private string? _lastAppliedFilter;

    // ─── Constructor ────────────────────────────────────────────────

    public FilterableComboBox()
    {
        IsEditable = true;
        IsTextSearchEnabled = false;
        StaysOpenOnEdit = true;
        IsSynchronizedWithCurrentItem = false;
    }

    // ─── Template ───────────────────────────────────────────────────

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _editBox = GetTemplateChild("PART_EditableTextBox") as TextBox;
    }

    // ─── Source changed → create per-instance view ──────────────────

    private static void OnFilterableItemsSourceChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var combo = (FilterableComboBox)d;

        if (e.NewValue is IEnumerable items)
        {
            // Accept IList directly (ObservableCollection implements IList)
            // so the view auto-updates on CollectionChanged.
            var list = items as IList
                       ?? items.Cast<object>().ToList();

            combo._view = new ListCollectionView(list);

            // Suppress WPF auto-selection during the synchronous
            // ItemsSource assignment only.
            combo._isLoadingItems = true;
            combo.ItemsSource = combo._view;
            combo._isLoadingItems = false;

            // After the full binding cycle completes, re-read from
            // the ViewModel.  Supports both SelectedItem and
            // SelectedValue/SelectedValuePath binding patterns.
            combo.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Loaded, () =>
            {
                // Re-read SelectedValue binding (used by MedicEditView)
                var valExpr = combo.GetBindingExpression(SelectedValueProperty);
                if (valExpr != null)
                {
                    valExpr.UpdateTarget();
                }

                // Re-read SelectedItem binding (used by MedicListView filters)
                var itemExpr = combo.GetBindingExpression(SelectedItemProperty);
                if (itemExpr != null)
                {
                    itemExpr.UpdateTarget();
                }

                // Only clear text if nothing is selected after re-reading
                if (combo.SelectedItem == null && combo.SelectedValue == null)
                {
                    combo.SelectedIndex = -1;
                    combo.Text = string.Empty;
                    if (combo._editBox != null)
                        combo._editBox.Text = string.Empty;
                }
                else
                {
                    // A value was restored — make sure text matches
                    combo.RestoreDisplayText();
                }
            });
        }
        else
        {
            combo._view = null;
            combo.ItemsSource = null;
        }
    }

    // ─── Keyboard handling ──────────────────────────────────────────

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (e.Key == Key.Escape)
        {
            ClearFilter();
            IsDropDownOpen = false;
            RestoreDisplayText();
            e.Handled = true;
        }
    }

    /// <summary>
    /// After each key-stroke that can change text (character input,
    /// Delete, Backspace), schedule a filter pass.
    /// </summary>
    protected override void OnPreviewTextInput(TextCompositionEventArgs e)
    {
        base.OnPreviewTextInput(e);
        Dispatcher.BeginInvoke(new Action(ApplyFilter));
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        if ((e.Key == Key.Delete || e.Key == Key.Back)
            && !_isSelecting && !_isResettingFilter)
        {
            ApplyFilter();
        }
    }

    // ─── Filtering logic ────────────────────────────────────────────

    private void ApplyFilter()
    {
        if (_isSelecting || _isResettingFilter || _view == null || _editBox == null)
            return;

        var filter = _editBox.Text?.Trim() ?? "";
        var displayPath = DisplayMemberPath;

        // Avoid redundant refresh
        if (filter == _lastAppliedFilter) return;
        _lastAppliedFilter = filter;

        if (string.IsNullOrEmpty(filter))
        {
            _view.Filter = null;
        }
        else
        {
            _view.Filter = item =>
            {
                var value = GetItemDisplayValue(item, displayPath);
                return value?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false;
            };
        }

        IsDropDownOpen = _view.Count > 0 && !string.IsNullOrEmpty(filter);
    }

    private void ClearFilter()
    {
        if (_view == null) return;

        _isResettingFilter = true;
        try
        {
            _view.Filter = null;
            _lastAppliedFilter = null;
        }
        finally
        {
            _isResettingFilter = false;
        }
    }

    // ─── Selection / focus events ───────────────────────────────────

    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        // Block auto-selection that WPF triggers when ItemsSource is set
        if (_isLoadingItems)
        {
            e.Handled = true;
            return;
        }

        _isSelecting = true;
        try
        {
            base.OnSelectionChanged(e);
            ClearFilter();
        }
        finally
        {
            _isSelecting = false;
        }
    }

    protected override void OnDropDownOpened(EventArgs e)
    {
        base.OnDropDownOpened(e);

        // Select all text so user can start typing immediately
        if (_editBox != null)
        {
            _editBox.SelectAll();
        }
    }

    protected override void OnDropDownClosed(EventArgs e)
    {
        base.OnDropDownClosed(e);
        ClearSelectionIfEditBoxEmpty();
        ClearFilter();
        RestoreDisplayText();
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        ClearSelectionIfEditBoxEmpty();
        ClearFilter();
        RestoreDisplayText();
    }

    // ─── Helpers ────────────────────────────────────────────────────

    /// <summary>
    /// If the user cleared the edit-box text (leaving it empty or whitespace),
    /// clear the SelectedItem and SelectedValue too. Without this, RestoreDisplayText
    /// would snap the text back to the still-set selection, and the SelectedValue
    /// binding would never fire — the bound property keeps its old value, and any
    /// "delete" the user attempted is silently lost on save.
    ///
    /// Called from OnLostFocus and OnDropDownClosed. NOT called from the Escape-key
    /// path, where the user's intent is "revert", not "clear".
    /// </summary>
    private void ClearSelectionIfEditBoxEmpty()
    {
        if (_editBox == null) return;
        if (!string.IsNullOrWhiteSpace(_editBox.Text)) return;

        // Reset the view filter via the existing helper (already guards re-entrancy).
        ClearFilter();

        // SelectedIndex = -1 nulls both SelectedItem and SelectedValue, which fires
        // the SelectedValue binding. RestoreDisplayText (called immediately after by
        // OnLostFocus / OnDropDownClosed) will see both null and leave the edit-box
        // empty via its existing else-branch.
        if (SelectedIndex != -1)
        {
            SelectedIndex = -1;
        }
    }

    /// <summary>
    /// After filtering or losing focus, sync the text-box back to
    /// the currently selected item's display value.
    /// </summary>
    private void RestoreDisplayText()
    {
        if (_editBox == null) return;

        _isSelecting = true;
        try
        {
            if (SelectedItem != null)
            {
                _editBox.Text = GetItemDisplayValue(SelectedItem, DisplayMemberPath) ?? "";
                _editBox.CaretIndex = _editBox.Text.Length;
            }
            else if (SelectedValue != null && !string.IsNullOrEmpty(SelectedValue.ToString()))
            {
                _editBox.Text = SelectedValue.ToString() ?? "";
                _editBox.CaretIndex = _editBox.Text.Length;
            }
            else
            {
                _editBox.Text = "";
            }
        }
        finally
        {
            _isSelecting = false;
        }
    }

    private static string? GetItemDisplayValue(object? item, string? path)
    {
        if (item == null) return null;
        if (string.IsNullOrEmpty(path)) return item.ToString();

        var prop = item.GetType().GetProperty(path, BindingFlags.Public | BindingFlags.Instance);
        return prop?.GetValue(item)?.ToString();
    }
}
