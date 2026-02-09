using System.Windows;
using System.Windows.Input;
using AVCNDB.WPF.ViewModels;

namespace AVCNDB.WPF;

/// <summary>
/// Fenêtre principale de l'application
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Sidebar_MouseEnter(object sender, MouseEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.IsMenuExpanded = true;
    }

    private void Sidebar_MouseLeave(object sender, MouseEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.IsMenuExpanded = false;
    }
}
