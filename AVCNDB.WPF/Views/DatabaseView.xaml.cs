using AVCNDB.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace AVCNDB.WPF.Views;

/// <summary>
/// Code-behind pour DatabaseView.xaml
/// </summary>
public partial class DatabaseView : System.Windows.Controls.UserControl
{
    public DatabaseView()
    {
        InitializeComponent();

        // DataContext is usually provided by navigation (DataTemplate).
        // We only ensure sub-ViewModels are wired once the DataContext is available.
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not DatabaseViewModel databaseViewModel)
        {
            return;
        }

        if (databaseViewModel.MedicListViewModel != null
            && databaseViewModel.DciListViewModel != null
            && databaseViewModel.FamiliesListViewModel != null
            && databaseViewModel.LabosListViewModel != null
            && databaseViewModel.InteractionsViewModel != null)
        {
            return;
        }

        var medicListViewModel = App.Services.GetRequiredService<MedicListViewModel>();
        var dciListViewModel = App.Services.GetRequiredService<DciListViewModel>();
        var familiesListViewModel = App.Services.GetRequiredService<FamiliesListViewModel>();
        var labosListViewModel = App.Services.GetRequiredService<LabosListViewModel>();
        var interactionsViewModel = App.Services.GetRequiredService<InteractionsViewModel>();

        databaseViewModel.InitializeSubViewModels(
            medicListViewModel,
            dciListViewModel,
            familiesListViewModel,
            labosListViewModel,
            interactionsViewModel);
    }
}
