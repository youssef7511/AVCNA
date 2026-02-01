using System.Windows;
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
}
