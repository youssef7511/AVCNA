using System.Windows;
using System.Windows.Controls;
using AVCNDB.WPF.ViewModels;

namespace AVCNDB.WPF.Views;

public partial class MovementsView : UserControl
{
    public MovementsView(EditionFileViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    /// <summary>
    /// Filter RadioButton Checked handler — sets ViewModel.FilterType from the button's Tag.
    /// Used because RadioButton IsChecked two-way binding to a string property is complex without a converter.
    /// </summary>
    private void FilterRadioButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && DataContext is EditionFileViewModel vm)
        {
            vm.FilterType = rb.Tag?.ToString() ?? "Tous";
        }
    }
}
