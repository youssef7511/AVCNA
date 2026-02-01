using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;

namespace AVCNDB.WPF.ViewModels;

/// <summary>
/// ViewModel de la page d'accueil
/// Affiche les statistiques et les raccourcis
/// </summary>
public partial class HomeViewModel : ViewModelBase
{
    private readonly IRepository<Models.Medic> _medicRepository;
    private readonly IRepository<Models.Dci> _dciRepository;
    private readonly IRepository<Models.Labos> _laboRepository;
    private readonly IStockService _stockService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private int _totalMedics;

    [ObservableProperty]
    private int _totalDcis;

    [ObservableProperty]
    private int _totalLabos;

    [ObservableProperty]
    private int _stockAlertsCount;

    [ObservableProperty]
    private int _expiryAlertsCount;

    [ObservableProperty]
    private IEnumerable<StockAlertItem> _recentAlerts = Enumerable.Empty<StockAlertItem>();

    public HomeViewModel(
        IRepository<Models.Medic> medicRepository,
        IRepository<Models.Dci> dciRepository,
        IRepository<Models.Labos> laboRepository,
        IStockService stockService,
        INavigationService navigationService)
    {
        _medicRepository = medicRepository;
        _dciRepository = dciRepository;
        _laboRepository = laboRepository;
        _stockService = stockService;
        _navigationService = navigationService;

        // Ne pas charger automatiquement les données - la base peut ne pas être disponible
        // L'utilisateur peut rafraîchir manuellement
    }

    private async Task LoadDashboardDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            try
            {
                // Charger les statistiques séquentiellement pour éviter les problèmes de concurrence DbContext
                TotalMedics = await _medicRepository.CountAsync();
                TotalDcis = await _dciRepository.CountAsync();
                TotalLabos = await _laboRepository.CountAsync();
                
                var stockAlerts = (await _stockService.GetLowStockAlertsAsync()).ToList();
                StockAlertsCount = stockAlerts.Count;
                RecentAlerts = stockAlerts.Take(5);

                ExpiryAlertsCount = (await _stockService.GetExpiryAlertsAsync()).Count();
            }
            catch (Exception)
            {
                // Base de données non disponible - afficher des valeurs par défaut
                TotalMedics = 0;
                TotalDcis = 0;
                TotalLabos = 0;
                StockAlertsCount = 0;
                ExpiryAlertsCount = 0;
                RecentAlerts = Enumerable.Empty<StockAlertItem>();
            }
        }, "Chargement du tableau de bord...");
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDashboardDataAsync();
    }

    [RelayCommand]
    private void NavigateToMedics()
    {
        _navigationService.NavigateTo<MedicListViewModel>();
    }

    [RelayCommand]
    private void NavigateToDcis()
    {
        _navigationService.NavigateTo<DciListViewModel>();
    }

    [RelayCommand]
    private void NavigateToLabos()
    {
        _navigationService.NavigateTo<LabosListViewModel>();
    }

    [RelayCommand]
    private void NavigateToStock()
    {
        _navigationService.NavigateTo<StockViewModel>();
    }
}
