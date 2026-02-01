using AVCNDB.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AVCNDB.WPF.Views;

/// <summary>
/// Code-behind pour DatabaseView.xaml
/// </summary>
public partial class DatabaseView : System.Windows.Controls.UserControl
{
    public DatabaseView()
    {
        InitializeComponent();
        
        // Récupérer les ViewModels depuis le conteneur DI
        var databaseViewModel = App.Services.GetRequiredService<DatabaseViewModel>();
        var medicListViewModel = App.Services.GetRequiredService<MedicListViewModel>();
        var dciListViewModel = App.Services.GetRequiredService<DciListViewModel>();
        var familiesListViewModel = App.Services.GetRequiredService<FamiliesListViewModel>();
        var labosListViewModel = App.Services.GetRequiredService<LabosListViewModel>();
        var interactionsViewModel = App.Services.GetRequiredService<InteractionsViewModel>();
        
        // Initialiser les sous-ViewModels
        databaseViewModel.InitializeSubViewModels(
            medicListViewModel,
            dciListViewModel,
            familiesListViewModel,
            labosListViewModel,
            interactionsViewModel);
        
        DataContext = databaseViewModel;
    }
}
