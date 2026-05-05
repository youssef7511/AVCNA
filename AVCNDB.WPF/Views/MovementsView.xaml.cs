using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AVCNDB.WPF.ViewModels;

namespace AVCNDB.WPF.Views;

public partial class MovementsView : UserControl
{
    public MovementsView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Filter RadioButton Checked handler — sets ViewModel.FilterType from the button's Tag.
    /// </summary>
    private void FilterRadioButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && DataContext is EditionFileViewModel vm)
        {
            vm.FilterType = rb.Tag?.ToString() ?? "Tous";
        }
    }

    /// <summary>
    /// Auto-select the DataGrid row when the PopupBox opens.
    /// Without this, SelectedRow stays null and all commands are disabled.
    /// </summary>
    private void PopupBox_Opened(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe) return;

        // Walk up the visual tree to find the DataGridRow
        DependencyObject current = fe;
        while (current != null && current is not DataGridRow)
        {
            current = VisualTreeHelper.GetParent(current);
        }

        if (current is DataGridRow row && row.DataContext is EditionRowViewModel rowVm
            && DataContext is EditionFileViewModel vm)
        {
            vm.SelectedRow = rowVm;
        }
    }
}
