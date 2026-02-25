using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;
using MaterialDesignThemes.Wpf;
using AVCNDB.WPF.Models;
using AVCNDB.WPF.ViewModels;

namespace AVCNDB.WPF.Views;

public partial class DciListView : UserControl
{
    public DciListView()
    {
        InitializeComponent();
    }

    private void DciGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not DciListViewModel vm || sender is not DataGrid grid)
        {
            return;
        }

        if (e.OriginalSource is not DependencyObject source)
        {
            return;
        }

        // Ignore action/checkbox zones; only open details on real row double-click.
        if (FindAncestor<CheckBox>(source) != null ||
            FindAncestor<Button>(source) != null ||
            FindAncestor<PopupBox>(source) != null ||
            FindAncestor<DataGridRow>(source) == null)
        {
            return;
        }

        if (grid.SelectedItem is Dci dci)
        {
            vm.OpenDetails(dci);
        }
    }

    private static T? FindAncestor<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child != null)
        {
            if (child is T typed)
            {
                return typed;
            }

            child = VisualTreeHelper.GetParent(child);
        }

        return null;
    }
}
