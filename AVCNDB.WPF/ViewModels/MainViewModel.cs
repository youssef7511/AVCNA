using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Services;

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
    private readonly ISessionService _sessionService;
    private readonly RememberMeService _rememberMeService;
    private readonly IAuthService _authService;

    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private string _currentPageTitle = "Accueil";

    [ObservableProperty]
    private bool _isHeaderVisible = true;

    [ObservableProperty]
    private bool _isMenuExpanded = true;

    [ObservableProperty]
    private int _alertsCount;

    [ObservableProperty]
    private bool _isDarkTheme;

    /// <summary>Nom d'utilisateur connecté affiché dans le chip du menu latéral.</summary>
    public string CurrentUsername =>
        _sessionService.CurrentUser?.username ?? string.Empty;

    public MainViewModel(
        INavigationService navigationService,
        IThemeService themeService,
        IStockService stockService,
        ISessionService sessionService,
        RememberMeService rememberMeService,
        IAuthService authService)
    {
        _navigationService = navigationService;
        _themeService = themeService;
        _stockService = stockService;
        _sessionService = sessionService;
        _rememberMeService = rememberMeService;
        _authService = authService;

        // S'abonner aux changements de navigation
        _navigationService.NavigationChanged += OnNavigationChanged;
        _themeService.ThemeChanged += OnThemeChanged;

        // Charger le thème enregistré et appliquer la palette aux brushes
        _themeService.Initialize();

        // Naviguer vers la page d'accueil par défaut
        NavigateToHome();
        
        // Charger les alertes
        _ = LoadAlertsAsync();
    }

    private void OnNavigationChanged()
    {
        CurrentView = _navigationService.CurrentView;
        UpdateHeaderVisibility();
    }

    partial void OnCurrentViewChanged(object? value)
    {
        UpdateHeaderVisibility();
    }

    private void UpdateHeaderVisibility()
    {
        // Masquer l'en-tête global - chaque page a déjà son propre en-tête
        IsHeaderVisible = false;
    }

    private void OnThemeChanged(AppTheme theme)
    {
        IsDarkTheme = theme == AppTheme.Dark;
    }

    private async Task LoadAlertsAsync()
    {
        try
        {
            AlertsCount = await _stockService.GetTotalAlertsCountAsync();
        }
        catch
        {
            AlertsCount = 0;
        }
    }

    [RelayCommand]
    private void NavigateToHome()
    {
        _navigationService.NavigateTo<HomeViewModel>();
        CurrentPageTitle = "Accueil";
        UpdateHeaderVisibility();
    }

    [RelayCommand]
    private void NavigateToLibrary()
    {
        _navigationService.NavigateTo<LibraryShellViewModel>();
        CurrentPageTitle = "Bibliothèque";
        UpdateHeaderVisibility();
    }

    [RelayCommand]
    private void NavigateToDatabase()
    {
        _navigationService.NavigateTo<DatabaseViewModel>();
        CurrentPageTitle = "Base de données";
        UpdateHeaderVisibility();
    }

    [RelayCommand]
    private void NavigateToMovements()
    {
        _navigationService.NavigateTo<EditionFileViewModel>();
        CurrentPageTitle = "Fichier d'édition";
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
    private void NavigateToTools()
    {
        _navigationService.NavigateTo<ToolsViewModel>();
        CurrentPageTitle = "Outils";
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

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var result = System.Windows.MessageBox.Show(
            "Voulez-vous vraiment vous déconnecter ?",
            "Déconnexion",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            try
            {
                var userId = _sessionService.CurrentUser?.recordid;
                if (userId.HasValue)
                    await _authService.RevokeRememberMeTokenAsync(userId.Value);
                _sessionService.Clear();
                _rememberMeService.Clear();
            }
            finally { System.Windows.Application.Current.Shutdown(); }
        }
    }
}
