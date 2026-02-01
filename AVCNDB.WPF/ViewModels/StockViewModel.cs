using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.ViewModels;

/// <summary>
/// ViewModel pour la gestion du stock avec alertes
/// </summary>
public partial class StockViewModel : ViewModelBase
{
    private readonly IStockService _stockService;
    private readonly IRepository<Stock> _repository;
    private readonly IDialogService _dialogService;
    private readonly IPdfService _pdfService;

    [ObservableProperty]
    private ObservableCollection<Stock> _stocks = new();

    [ObservableProperty]
    private Stock? _selectedStock;

    [ObservableProperty]
    private ObservableCollection<StockAlertItem> _lowStockAlerts = new();

    [ObservableProperty]
    private ObservableCollection<ExpiryAlertItem> _expiryAlerts = new();

    [ObservableProperty]
    private int _totalAlertsCount;

    [ObservableProperty]
    private int _lowStockCount;

    [ObservableProperty]
    private int _expiryCount;

    [ObservableProperty]
    private string _searchText = string.Empty;

    // Vue sélectionnée: "all", "alerts", "expiring"
    [ObservableProperty]
    private string _selectedView = "all";

    public StockViewModel(
        IStockService stockService,
        IRepository<Stock> repository,
        IDialogService dialogService,
        IPdfService pdfService)
    {
        _stockService = stockService;
        _repository = repository;
        _dialogService = dialogService;
        _pdfService = pdfService;

        _ = LoadDataAsync();
    }

    partial void OnSelectedViewChanged(string value)
    {
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Charger les alertes
            var lowStock = (await _stockService.GetLowStockAlertsAsync()).ToList();
            var expiring = (await _stockService.GetExpiryAlertsAsync()).ToList();

            LowStockAlerts = new ObservableCollection<StockAlertItem>(lowStock);
            ExpiryAlerts = new ObservableCollection<ExpiryAlertItem>(expiring);
            LowStockCount = lowStock.Count;
            ExpiryCount = expiring.Count;
            TotalAlertsCount = lowStock.Count + expiring.Count;

            // Charger selon la vue sélectionnée
            IEnumerable<Stock> items;
            
            switch (SelectedView)
            {
                case "alerts":
                    var alertMedicIds = lowStock.Select(a => a.MedicId).ToHashSet();
                    items = await _repository.FindAsync(s => alertMedicIds.Contains(s.medicid));
                    break;
                    
                case "expiring":
                    var expiringMedicIds = expiring.Select(a => a.MedicId).ToHashSet();
                    items = await _repository.FindAsync(s => expiringMedicIds.Contains(s.medicid));
                    break;
                    
                default: // "all"
                    items = string.IsNullOrEmpty(SearchText)
                        ? await _repository.GetAllAsync()
                        : await _repository.FindAsync(s => s.medicname.Contains(SearchText));
                    break;
            }

            Stocks = new ObservableCollection<Stock>(items.OrderBy(s => s.medicname));
        }, "Chargement du stock...");
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private void ShowAll()
    {
        SelectedView = "all";
    }

    [RelayCommand]
    private void ShowAlertsOnly()
    {
        SelectedView = "alerts";
    }

    [RelayCommand]
    private void ShowExpiringOnly()
    {
        SelectedView = "expiring";
    }

    [RelayCommand]
    private async Task UpdateQuantityAsync()
    {
        if (SelectedStock == null) return;

        // TODO: Implémenter un dialogue de saisie personnalisé
        var newQuantity = SelectedStock.quantity; // Placeholder

        await ExecuteAsync(async () =>
        {
            await _stockService.UpdateStockAsync(SelectedStock.medicid, newQuantity);
            await LoadDataAsync();
            await _dialogService.ShowSuccessAsync("Succès", "Quantité mise à jour.");
        });
    }

    [RelayCommand]
    private async Task AddStockAsync()
    {
        if (SelectedStock == null) return;

        // TODO: Implémenter un dialogue de saisie personnalisé pour quantité, lot, expiration
        await _dialogService.ShowInfoAsync("Ajouter du stock", 
            "Fonctionnalité à implémenter avec un dialogue personnalisé.");
    }

    [RelayCommand]
    private async Task SetAlertThresholdsAsync()
    {
        if (SelectedStock == null) return;

        // TODO: Implémenter un dialogue de saisie personnalisé
        await _dialogService.ShowInfoAsync("Seuils d'alerte", 
            "Fonctionnalité à implémenter avec un dialogue personnalisé.");
    }

    [RelayCommand]
    private async Task ExportReportAsync()
    {
        var filePath = _dialogService.ShowSaveFileDialog(
            "PDF Files|*.pdf",
            $"Rapport_Stock_{DateTime.Now:yyyyMMdd}",
            "Exporter le rapport de stock");

        if (!string.IsNullOrEmpty(filePath))
        {
            await ExecuteAsync(async () =>
            {
                await _pdfService.GenerateStockReportAsync(filePath, true);
                await _dialogService.ShowSuccessAsync("Export réussi", 
                    $"Rapport de stock exporté vers {filePath}");
            }, "Génération du rapport...");
        }
    }
}
