using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.ViewModels;

/// <summary>
/// ViewModel pour la page Base de données
/// Gère les sous-ViewModels pour chaque table
/// </summary>
public partial class DatabaseViewModel : ViewModelBase
{
    private readonly IRepository<Medic> _medicRepository;
    private readonly IRepository<Dci> _dciRepository;
    private readonly IRepository<Families> _familiesRepository;
    private readonly IRepository<Labos> _labosRepository;
    private readonly IRepository<Interact> _interactRepository;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private MedicListViewModel? _medicListViewModel;

    [ObservableProperty]
    private DciListViewModel? _dciListViewModel;

    [ObservableProperty]
    private FamiliesListViewModel? _familiesListViewModel;

    [ObservableProperty]
    private LabosListViewModel? _labosListViewModel;

    [ObservableProperty]
    private InteractionsViewModel? _interactionsViewModel;

    public DatabaseViewModel(
        IRepository<Medic> medicRepository,
        IRepository<Dci> dciRepository,
        IRepository<Families> familiesRepository,
        IRepository<Labos> labosRepository,
        IRepository<Interact> interactRepository,
        INavigationService navigationService)
    {
        _medicRepository = medicRepository;
        _dciRepository = dciRepository;
        _familiesRepository = familiesRepository;
        _labosRepository = labosRepository;
        _interactRepository = interactRepository;
        _navigationService = navigationService;

        // Les ViewModels seront injectés via le conteneur DI
    }

    /// <summary>
    /// Initialise les sous-ViewModels
    /// </summary>
    public void InitializeSubViewModels(
        MedicListViewModel medicListViewModel,
        DciListViewModel dciListViewModel,
        FamiliesListViewModel familiesListViewModel,
        LabosListViewModel labosListViewModel,
        InteractionsViewModel interactionsViewModel)
    {
        MedicListViewModel = medicListViewModel;
        DciListViewModel = dciListViewModel;
        FamiliesListViewModel = familiesListViewModel;
        LabosListViewModel = labosListViewModel;
        InteractionsViewModel = interactionsViewModel;
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
