using System.Windows.Controls;
using AVCNDB.WPF.ViewModels;

namespace AVCNDB.WPF.Views;

public partial class InteractionsView : UserControl
{
    public InteractionsView()
    {
        InitializeComponent();
    }

    private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
    {
        (DataContext as InteractionsViewModel)?.CancelInFlightAnalysis();
    }
}
