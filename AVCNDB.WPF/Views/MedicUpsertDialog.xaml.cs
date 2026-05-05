using System.ComponentModel;
using System.Windows;
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
}
