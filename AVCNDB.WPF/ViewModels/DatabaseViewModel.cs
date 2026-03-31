using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;

namespace AVCNDB.WPF.ViewModels;

/// <summary>
/// ViewModel pour la page Base de données
/// Gère les sous-ViewModels pour chaque table
/// </summary>
public partial class DatabaseViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private MedicListViewModel _medicListViewModel;

    [ObservableProperty]
    private InteractionsViewModel _interactionsViewModel;

    [ObservableProperty]
    private DenominationViewModel _denominationViewModel;

    [ObservableProperty]
    private PrixViewModel _prixViewModel;

    [ObservableProperty]
    private MonographieViewModel _monographieViewModel;

    public DatabaseViewModel(
        INavigationService navigationService,
        MedicListViewModel medicListViewModel,
        InteractionsViewModel interactionsViewModel,
        DenominationViewModel denominationViewModel,
        PrixViewModel prixViewModel,
        MonographieViewModel monographieViewModel)
    {
        _navigationService = navigationService;
        MedicListViewModel = medicListViewModel;
        InteractionsViewModel = interactionsViewModel;
        DenominationViewModel = denominationViewModel;
        PrixViewModel = prixViewModel;
        MonographieViewModel = monographieViewModel;
    }

    /// <summary>
    /// Navigue vers un onglet spécifique
    /// </summary>
    [RelayCommand]
    private void NavigateToTab(int tabIndex)
    {
        SelectedTabIndex = tabIndex;
    }
}
