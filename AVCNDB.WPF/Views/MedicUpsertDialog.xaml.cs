using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using AVCNDB.WPF.Controls;
using AVCNDB.WPF.Models;
using AVCNDB.WPF.ViewModels;

namespace AVCNDB.WPF.Views;

public partial class MedicUpsertDialog : Window
{
    private readonly MedicUpsertDialogViewModel _viewModel;

    // Prevents the OnClosing guard from re-triggering after the user confirms discard
    // or after a successful save.
    private bool _forceClose;

    public MedicUpsertDialog(MedicUpsertDialogViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;

        InitializeComponent();

        _viewModel.RequestClose += OnRequestClose;
    }

    public async Task InitializeAsync(int? medicId)
    {
        await _viewModel.InitializeAsync(medicId);
    }

    private void OnRequestClose(bool saved)
    {
        _forceClose = true;
        DialogResult = saved;
        Close();
    }

    /// <summary>
    /// Intercept every close attempt (X button, Alt+F4, etc.).
    /// If there are unsaved changes, ask the user whether to discard them.
    /// </summary>
    protected override async void OnClosing(CancelEventArgs e)
    {
        // Let programmatic closes (from SaveCommand / CancelCommand) pass through.
        if (_forceClose)
        {
            base.OnClosing(e);
            return;
        }

        if (_viewModel.HasUnsavedChanges)
        {
            // Cancel the close immediately; decide asynchronously.
            e.Cancel = true;

            var discard = await _viewModel.ConfirmDiscardAsync();
            if (discard)
            {
                _forceClose = true;
                Close();
            }
            // else: user chose to stay — do nothing.
        }
        else
        {
            base.OnClosing(e);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.RequestClose -= OnRequestClose;
        base.OnClosed(e);
    }

    /// <summary>
    /// Force the currently-focused control to lose focus BEFORE the SaveCommand fires,
    /// then synchronously flush every FilterableComboBox binding in the dialog.
    ///
    /// Background: Keyboard.Focus(this) fires LostFocus on the active combobox, which
    /// calls ClearSelectionIfEditBoxEmpty → SetCurrentValue(SelectedValueProperty, "").
    /// For ComboBox.SelectedValue with SelectedValuePath, WPF's coercion can defer the
    /// TwoWay binding write-back (source update) until after the current call stack
    /// unwinds — meaning SaveCommand.Execute sees the stale Medic.fam1 value and
    /// Validate() passes even though the user cleared the field.  Calling UpdateSource()
    /// on every FilterableComboBox.SelectedValueProperty binding expression here pushes
    /// the current DP value to the ViewModel synchronously, closing the race for all
    /// comboboxes in the dialog (Fam1..4, Dci1..4, Labo, Forme, Voie, Présentation).
    /// </summary>
    private void SaveButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Step 1: commit any in-flight LostFocus / ClearSelectionIfEditBoxEmpty on the
        // currently-focused FilterableComboBox.
        Keyboard.Focus(this);

        // Step 2: flush SelectedValue bindings on every FilterableComboBox in the dialog
        // so that the ViewModel's source properties are up-to-date before SaveCommand fires.
        foreach (var combo in GetDescendants<FilterableComboBox>(this))
        {
            BindingOperations
                .GetBindingExpression(combo, FilterableComboBox.SelectedValueProperty)
                ?.UpdateSource();
        }
    }

    /// <summary>Enumerates all logical-tree descendants of <paramref name="root"/> that are of type <typeparamref name="T"/>.</summary>
    private static IEnumerable<T> GetDescendants<T>(DependencyObject root) where T : DependencyObject
    {
        var queue = new Queue<DependencyObject>();
        queue.Enqueue(root);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(current);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(current, i);
                if (child is T match) yield return match;
                queue.Enqueue(child);
            }
        }
    }

    /// <summary>
    /// Inserts the selected Rubrique as a Markdown H2 heading at the caret position
    /// of the Monographie editor. Caret stays at the end of the inserted block so the
    /// user can immediately type the section content.
    /// </summary>
    private void InsertRubriqueButton_Click(object sender, RoutedEventArgs e)
    {
        var rubrique = _viewModel.SelectedRubrique;
        if (string.IsNullOrWhiteSpace(rubrique)) return;

        var editor = MonographieEditor;
        var caret = editor.CaretIndex;
        var current = editor.Text ?? string.Empty;

        // Ensure the inserted block is preceded by a blank line (Markdown requires it
        // for headings to register). Skip leading newlines if we are already at start
        // of a blank line.
        var prefix = (caret == 0 || current.Substring(0, caret).EndsWith("\n\n"))
            ? string.Empty
            : (current.Substring(0, caret).EndsWith("\n") ? "\n" : "\n\n");

        var insertion = $"{prefix}## {rubrique}\n\n";
        editor.Text = current.Insert(caret, insertion);
        editor.CaretIndex = caret + insertion.Length;
        editor.Focus();
    }

    /// <summary>
    /// Opens a read-only detail dialog for the double-clicked interaction row.
    /// Reads the full description / mecanisme / conduite without ellipsis,
    /// which the compact DataGrid columns can't show comfortably.
    /// </summary>
    private void MedicInteractionsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid) return;
        if (grid.SelectedItem is not Interact interact) return;

        var dialog = new InteractionDetailsDialog(interact) { Owner = this };
        dialog.ShowDialog();
    }
}
