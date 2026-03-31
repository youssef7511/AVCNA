using System.Windows.Controls;
using AVCNDB.WPF.ViewModels;

namespace AVCNDB.WPF.Views;

public partial class PrixView : UserControl
{
    public PrixView()
    {
        InitializeComponent();
    }

    private void DataGrid_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Commit
            && DataContext is PrixViewModel vm)
        {
            vm.NotifyCellEdited();
        }
    }
}
