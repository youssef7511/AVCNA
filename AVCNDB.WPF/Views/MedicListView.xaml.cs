using System.Windows.Controls;
using AVCNDB.WPF.Controls;
using AVCNDB.WPF.Models;
using AVCNDB.WPF.ViewModels;

namespace AVCNDB.WPF.Views;

public partial class MedicListView : UserControl
{
    public MedicListView()
    {
        InitializeComponent();
    }

    private void LibraryComboBox_DropDownClosed(object sender, EventArgs e)
    {
        if (sender is ComboBox cb
            && cb.SelectedItem != null
            && cb.DataContext is Medic medic
            && DataContext is MedicListViewModel vm)
        {
            vm.SaveInlineEditCommand.Execute(medic);
        }
    }
}
