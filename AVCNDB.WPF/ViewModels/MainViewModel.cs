using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;

namespace AVCNDB.WPF.ViewModels;

/// <summary>
/// ViewModel principal de l'application
/// Gère la navigation et l'état global
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IThemeService _themeService;
    private readonly IStockService _stockService;

    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private string _currentPageTitle = "Accueil";

    [ObservableProperty]
    private bool _isMenuExpanded = true;

    [ObservableProperty]
    private int _alertsCount;

    [ObservableProperty]
    private bool _isDarkTheme;

    public MainViewModel(
        INavigationService navigationService,
        IThemeService themeService,
        IStockService stockService)
    {
        _navigationService = navigationService;
        _themeService = themeService;
        _stockService = stockService;

        // S'abonner aux changements de navigation
        _navigationService.NavigationChanged += OnNavigationChanged;
        _themeService.ThemeChanged += OnThemeChanged;

        // Naviguer vers la page d'accueil par défaut
        NavigateToHome();
        
        // Charger les alertes
        _ = LoadAlertsAsync();
    }

    private void OnNavigationChanged()
    {
        CurrentView = _navigationService.CurrentView;
    }

    private void OnThemeChanged(AppTheme theme)
    {
        IsDarkTheme = theme == AppTheme.Dark;
    }

    private async Task LoadAlertsAsync()
    {
        AlertsCount = await _stockService.GetTotalAlertsCountAsync();
    }

    [RelayCommand]
    private void NavigateToHome()
    {
        _navigationService.NavigateTo<HomeViewModel>();
        CurrentPageTitle = "Accueil";
    }

    [RelayCommand]
    private void NavigateToMedics()
    {
        _navigationService.NavigateTo<MedicListViewModel>();
        CurrentPageTitle = "Médicaments";
    }

    [RelayCommand]
    private void NavigateToDci()
    {
        _navigationService.NavigateTo<DciListViewModel>();
        CurrentPageTitle = "DCI (Substances Actives)";
    }

    [RelayCommand]
    private void NavigateToFamilies()
    {
        _navigationService.NavigateTo<FamiliesListViewModel>();
        CurrentPageTitle = "Familles Thérapeutiques";
    }

    [RelayCommand]
    private void NavigateToLabos()
    {
        _navigationService.NavigateTo<LabosListViewModel>();
        CurrentPageTitle = "Laboratoires";
    }

    [RelayCommand]
    private void NavigateToInteractions()
    {
        _navigationService.NavigateTo<InteractionsViewModel>();
        CurrentPageTitle = "Interactions";
    }

    [RelayCommand]
    private void NavigateToStock()
    {
        _navigationService.NavigateTo<StockViewModel>();
        CurrentPageTitle = "Gestion du Stock";
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        _navigationService.NavigateTo<SettingsViewModel>();
        CurrentPageTitle = "Paramètres";
    }

    [RelayCommand]
    private void ToggleMenu()
    {
        IsMenuExpanded = !IsMenuExpanded;
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        _themeService.ToggleTheme();
    }

    [RelayCommand]
    private void GoBack()
    {
        if (_navigationService.CanGoBack)
        {
            _navigationService.GoBack();
        }
    }
}
