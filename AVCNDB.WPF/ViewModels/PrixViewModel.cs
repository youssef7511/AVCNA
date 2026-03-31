using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.ViewModels;

/// <summary>
/// ViewModel pour la gestion et l'édition collective des prix.
/// Charge tous les médicaments et expose un DataGrid éditable focalisé sur les colonnes de prix.
/// </summary>
public partial class PrixViewModel : ViewModelBase
{
    private readonly IRepository<Medic> _repository;
    private readonly IDialogService _dialogService;

    private ObservableCollection<Medic> _allMedics = new();

    // ============================================
    // OBSERVABLE PROPERTIES
    // ============================================

    [ObservableProperty]
    private ICollectionView? _medicsView;

    [ObservableProperty]
    private Medic? _selectedMedic;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _filteredCount;

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    [ObservableProperty]
    private string _activeFilterText = "Tout afficher";

    private PriceFilterMode _activeFilter = PriceFilterMode.All;

    // ============================================
    // CONSTRUCTOR
    // ============================================

    public PrixViewModel(
        IRepository<Medic> repository,
        IDialogService dialogService)
    {
        _repository = repository;
        _dialogService = dialogService;

        _ = LoadDataAsync();
    }

    public override void OnNavigatedTo(object? parameter)
    {
        // Data already loaded in constructor; no-op.
    }

    // ============================================
    // LOAD DATA
    // ============================================

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            var medics = await _repository.GetAllAsync();
            _allMedics = new ObservableCollection<Medic>(medics);
            TotalCount = _allMedics.Count;

            MedicsView = CollectionViewSource.GetDefaultView(_allMedics);
            MedicsView.Filter = FilterMedic;
            UpdateFilteredCount();
        }, "Chargement des prix...");
    }

    // ============================================
    // FILTERING
    // ============================================

    private bool FilterMedic(object obj)
    {
        if (obj is not Medic m) return false;

        // Active filter
        switch (_activeFilter)
        {
            case PriceFilterMode.Inconsistencies:
                // PVP PCT should equal Prix PCT + Timbre
                if (m.netprice == m.pctprice + m.timbrepct) return false;
                break;
            case PriceFilterMode.EmptyPrices:
                if (m.price > 0 && m.pctprice > 0) return false;
                break;
            case PriceFilterMode.CnamReferences:
                if (m.refprice <= 0) return false;
                break;
        }

        // Search text
        if (string.IsNullOrWhiteSpace(SearchText)) return true;
        var term = SearchText.Trim();
        return m.pctcode.Contains(term, StringComparison.OrdinalIgnoreCase)
            || m.itemname.Contains(term, StringComparison.OrdinalIgnoreCase)
            || m.labo.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    partial void OnSearchTextChanged(string value)
    {
        DebounceSearch(() =>
        {
            MedicsView?.Refresh();
            UpdateFilteredCount();
            return Task.CompletedTask;
        });
    }

    private void UpdateFilteredCount()
    {
        if (MedicsView is ICollectionView cv)
        {
            FilteredCount = cv.Cast<object>().Count();
        }
    }

    private void ApplyFilter(PriceFilterMode mode, string label)
    {
        _activeFilter = mode;
        ActiveFilterText = label;
        MedicsView?.Refresh();
        UpdateFilteredCount();
    }

    /// <summary>
    /// Called by code-behind when a DataGrid cell is manually edited.
    /// </summary>
    public void NotifyCellEdited()
    {
        HasUnsavedChanges = true;
    }

    // ============================================
    // TOOLBAR — FILTER COMMANDS
    // ============================================

    /// <summary>Tout afficher — clear all filters.</summary>
    [RelayCommand]
    private void ShowAll()
    {
        SearchText = string.Empty;
        ApplyFilter(PriceFilterMode.All, "Tout afficher");
    }

    /// <summary>Afficher les incohérences de prix (PVP PCT ≠ Prix PCT + Timbre).</summary>
    [RelayCommand]
    private void ShowInconsistencies()
    {
        ApplyFilter(PriceFilterMode.Inconsistencies, "Incohérences de prix");
    }

    /// <summary>Afficher les prix vides (price = 0 ou pctprice = 0).</summary>
    [RelayCommand]
    private void ShowEmptyPrices()
    {
        ApplyFilter(PriceFilterMode.EmptyPrices, "Prix vides");
    }

    /// <summary>Afficher les références CNAM (refprice > 0).</summary>
    [RelayCommand]
    private void ShowCnamReferences()
    {
        ApplyFilter(PriceFilterMode.CnamReferences, "Références CNAM");
    }

    // ============================================
    // TOOLBAR — ACTION COMMANDS
    // ============================================

    /// <summary>
    /// Optimiser les prix — recalculate PVP PCT = Prix PCT + Timbre for all visible medics.
    /// </summary>
    [RelayCommand]
    private async Task OptimizePricesAsync()
    {
        var confirmed = await _dialogService.ShowConfirmAsync(
            "Optimisation des prix",
            "Recalculer PVP PCT = Prix PCT + Timbre pour tous les médicaments affichés ?\n" +
            "Les modifications devront être sauvegardées avec 'Appliquer'.");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            int count = 0;
            var visible = MedicsView?.Cast<Medic>().ToList() ?? _allMedics.ToList();

            foreach (var m in visible)
            {
                var optimized = m.pctprice + m.timbrepct;
                if (m.netprice != optimized)
                {
                    m.netprice = optimized;
                    count++;
                }
            }

            HasUnsavedChanges = count > 0 || HasUnsavedChanges;
            MedicsView?.Refresh();

            await _dialogService.ShowInfoAsync(
                "Optimisation terminée",
                $"{count} PVP PCT recalculé(s).\nCliquez 'Appliquer' pour sauvegarder.");
        }, "Optimisation en cours...");
    }

    /// <summary>
    /// Appliquer — save all modified medics to the database.
    /// </summary>
    [RelayCommand]
    private async Task ApplyPricesAsync()
    {
        if (!HasUnsavedChanges)
        {
            await _dialogService.ShowInfoAsync("Information", "Aucune modification à sauvegarder.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmAsync(
            "Sauvegarder",
            "Voulez-vous appliquer toutes les modifications de prix à la base de données ?");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            int saved = 0;
            foreach (var m in _allMedics)
            {
                await _repository.UpdateAsync(m);
                saved++;
            }

            HasUnsavedChanges = false;

            await _dialogService.ShowSuccessAsync(
                "Sauvegarde réussie",
                $"{saved} médicament(s) mis à jour dans la base de données.");
        }, "Sauvegarde en cours...");
    }

    // ============================================
    // INNER ENUM
    // ============================================

    private enum PriceFilterMode
    {
        All,
        Inconsistencies,
        EmptyPrices,
        CnamReferences
    }
}
