using System.Windows;
using AVCNDB.WPF.ViewModels;

namespace AVCNDB.WPF.Views;

public partial class MedicUpsertDialog : Window
{
    private readonly MedicUpsertDialogViewModel _viewModel;

    public MedicUpsertDialog(MedicUpsertDialogViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;

        InitializeComponent();

        _viewModel.RequestClose += OnRequestClose;
    }

    /// <summary>
    /// Call after construction to load data and set edit/new mode.
    /// </summary>
    public async Task InitializeAsync(int? medicId)
    {
        await _viewModel.InitializeAsync(medicId);
    }

    private void OnRequestClose(bool saved)
    {
        DialogResult = saved;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.RequestClose -= OnRequestClose;
        base.OnClosed(e);
    }
}
