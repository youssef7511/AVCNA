using System.Windows;
using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.Views;

public partial class InteractionDetailsDialog : Window
{
    public InteractionDetailsDialog(Interact interaction)
    {
        InitializeComponent();
        DataContext = interaction;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
