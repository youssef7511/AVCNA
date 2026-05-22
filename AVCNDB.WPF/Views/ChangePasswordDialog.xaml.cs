using System.Windows;
using AVCNDB.WPF.ViewModels;

namespace AVCNDB.WPF.Views;

/// <summary>Dialogue de changement de mot de passe (premier lancement ou changement explicite).</summary>
public partial class ChangePasswordDialog : Window
{
    private readonly ChangePasswordDialogViewModel _viewModel;

    public ChangePasswordDialog(ChangePasswordDialogViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        InitializeComponent();
        _viewModel.RequestClose += OnRequestClose;
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
