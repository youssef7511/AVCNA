using System.Windows;
using AVCNDB.WPF.Models;
using AVCNDB.WPF.ViewModels;

namespace AVCNDB.WPF.Views;

public partial class SimilaritySearchDialog : Window
{
    private readonly SimilaritySearchViewModel _viewModel;

    public SimilaritySearchDialog(SimilaritySearchViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;

        InitializeComponent();

        _viewModel.RequestClose += OnRequestClose;
    }

    public async Task InitializeAsync(EditionRow row)
    {
        await _viewModel.InitializeAsync(row);
    }

    private void OnRequestClose(bool result)
    {
        DialogResult = result;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.RequestClose -= OnRequestClose;
        base.OnClosed(e);
    }
}
