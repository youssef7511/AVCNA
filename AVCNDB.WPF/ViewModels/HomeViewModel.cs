using System.Globalization;
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

    [ObservableProperty]
    private string _lastRefreshedText = string.Empty;

    [ObservableProperty]
    private IEnumerable<MonthBarItem> _monthlyMedicData = Enumerable.Empty<MonthBarItem>();

    [ObservableProperty]
    private IEnumerable<TopItem> _topFamilies = Enumerable.Empty<TopItem>();

    [ObservableProperty]
    private IEnumerable<TopItem> _topLabos = Enumerable.Empty<TopItem>();

    [ObservableProperty]
    private double _stockCoveragePercent;

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
    }

    public override void OnNavigatedTo(object? parameter)
    {
        _ = LoadDashboardDataAsync();
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
                LastRefreshedText = $"Mis à jour à {DateTime.Now:HH:mm}";

                // ── Chart data ──────────────────────────────────────────
                var allMedics = (await _medicRepository.GetAllAsync()).ToList();

                // Monthly bar chart – last 12 months
                var monthGroups = allMedics
                    .Where(m => m.addedat.HasValue && m.addedat.Value >= DateTime.Now.AddMonths(-12))
                    .GroupBy(m => (m.addedat!.Value.Year, m.addedat.Value.Month))
                    .ToDictionary(g => g.Key, g => g.Count());

                var fr = CultureInfo.GetCultureInfo("fr-FR");
                var months = new List<MonthBarItem>();
                for (int i = 11; i >= 0; i--)
                {
                    var dt = DateTime.Now.AddMonths(-i);
                    months.Add(new MonthBarItem
                    {
                        Month = dt.ToString("MMM", fr),
                        Count = monthGroups.TryGetValue((dt.Year, dt.Month), out var c) ? c : 0
                    });
                }
                var maxMonth = months.Max(m => m.Count);
                if (maxMonth > 0)
                    months.ForEach(m => m.Ratio = (double)m.Count / maxMonth);
                MonthlyMedicData = months;

                // Top 5 families
                var topFams = allMedics
                    .Where(m => !string.IsNullOrWhiteSpace(m.fam1))
                    .GroupBy(m => m.fam1.Trim())
                    .Select(g => new TopItem { Name = g.Key, Count = g.Count() })
                    .OrderByDescending(f => f.Count)
                    .Take(5)
                    .ToList();
                if (topFams.Count > 0)
                {
                    var maxFam = topFams.Max(f => f.Count);
                    topFams.ForEach(f => f.Ratio = (double)f.Count / maxFam);
                }
                TopFamilies = topFams;

                // Top 5 labos by medic count
                var topLabos = allMedics
                    .Where(m => !string.IsNullOrWhiteSpace(m.labo))
                    .GroupBy(m => m.labo.Trim())
                    .Select(g => new TopItem { Name = g.Key, Count = g.Count() })
                    .OrderByDescending(l => l.Count)
                    .Take(5)
                    .ToList();
                if (topLabos.Count > 0)
                {
                    var maxLabo = topLabos.Max(l => l.Count);
                    topLabos.ForEach(l => l.Ratio = (double)l.Count / maxLabo);
                }
                TopLabos = topLabos;

                // Stock coverage %
                StockCoveragePercent = TotalMedics > 0
                    ? Math.Round((TotalMedics - StockAlertsCount) * 100.0 / TotalMedics, 1)
                    : 100.0;
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
                MonthlyMedicData = Enumerable.Empty<MonthBarItem>();
                TopFamilies = Enumerable.Empty<TopItem>();
                TopLabos = Enumerable.Empty<TopItem>();
                StockCoveragePercent = 0;
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

    [RelayCommand]
    private void NavigateToImport()
    {
        _navigationService.NavigateTo<EditionFileViewModel>();
    }

    [RelayCommand]
    private void NavigateToFamilies()
    {
        _navigationService.NavigateTo<FamiliesListViewModel>();
    }

    [RelayCommand]
    private void NavigateToInteractions()
    {
        _navigationService.NavigateTo<InteractionsViewModel>();
    }
}

// ── Dashboard-only data models ─────────────────────────────────────────────

public class MonthBarItem
{
    public string Month { get; set; } = string.Empty;
    public int    Count { get; set; }
    public double Ratio { get; set; } // 0.0 – 1.0 relative to the monthly maximum
}

public class TopItem
{
    public string Name  { get; set; } = string.Empty;
    public int    Count { get; set; }
    public double Ratio { get; set; } // 0.0 – 1.0 relative to the top item
}
