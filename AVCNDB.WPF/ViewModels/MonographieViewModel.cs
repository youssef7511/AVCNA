using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Messages;
using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.ViewModels;

/// <summary>
/// ViewModel pour la gestion et l'édition collective des monographies.
/// Charge tous les médicaments et expose un DataGrid éditable focalisé sur
/// les colonnes de monographie : DCI, forme, voie, groupe de formes, dates.
/// </summary>
public partial class MonographieViewModel : ViewModelBase
{
    private readonly IRepository<Medic> _repository;
    private readonly IRepository<Voies> _voiesRepository;
    private readonly IRepository<Dci> _dciRepository;
    private readonly IRepository<Formes> _formesRepository;
    private readonly IRepository<Labos> _labosRepository;
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

    [ObservableProperty]
    private ObservableCollection<string> _voiesList = new();

    [ObservableProperty]
    private ObservableCollection<string> _dciList = new();

    [ObservableProperty]
    private ObservableCollection<string> _formesList = new();

    [ObservableProperty]
    private ObservableCollection<string> _labosList = new();

    [ObservableProperty]
    private ObservableCollection<string> _formGroupsList = new();

    private MonographFilterMode _activeFilter = MonographFilterMode.All;

    // ============================================
    // CONSTRUCTOR
    // ============================================

    public MonographieViewModel(
        IRepository<Medic> repository,
        IRepository<Voies> voiesRepository,
        IRepository<Dci> dciRepository,
        IRepository<Formes> formesRepository,
        IRepository<Labos> labosRepository,
        IDialogService dialogService)
    {
        _repository = repository;
        _voiesRepository = voiesRepository;
        _dciRepository = dciRepository;
        _formesRepository = formesRepository;
        _labosRepository = labosRepository;
        _dialogService = dialogService;

        // Refresh library lists when data changes
        WeakReferenceMessenger.Default.Register<DataChangedMessage>(this, (r, m) =>
        {
            var entity = m.Value.EntityType;
            if (entity is "Voies" or "Dci" or "Formes" or "Labos")
            {
                App.Current.Dispatcher.InvokeAsync(async () => await LoadLibraryListsAsync());
            }
        });

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
            await LoadLibraryListsAsync();

            var medics = await _repository.GetAllAsync();
            _allMedics = new ObservableCollection<Medic>(medics);
            TotalCount = _allMedics.Count;

            MedicsView = CollectionViewSource.GetDefaultView(_allMedics);
            MedicsView.Filter = FilterMedic;
            UpdateFilteredCount();
        }, "Chargement des monographies...");
    }

    /// <summary>
    /// Loads all library lists (Voies, DCI, Formes, Labos) from their repositories.
    /// </summary>
    private async Task LoadLibraryListsAsync()
    {
        var voies = await _voiesRepository.GetAllAsync();
        VoiesList = new ObservableCollection<string>(
            voies.Select(v => v.itemname)
                 .Where(n => !string.IsNullOrWhiteSpace(n))
                 .Distinct()
                 .OrderBy(n => n));

        var dcis = await _dciRepository.GetAllAsync();
        DciList = new ObservableCollection<string>(
            dcis.Select(d => d.itemname)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .OrderBy(n => n));

        var formes = await _formesRepository.GetAllAsync();
        FormesList = new ObservableCollection<string>(
            formes.Select(f => f.itemname)
                  .Where(n => !string.IsNullOrWhiteSpace(n))
                  .Distinct()
                  .OrderBy(n => n));

        FormGroupsList = new ObservableCollection<string>(
            formes.Select(f => f.formgroup)
                  .Where(n => !string.IsNullOrWhiteSpace(n))
                  .Distinct()
                  .OrderBy(n => n));

        var labos = await _labosRepository.GetAllAsync();
        LabosList = new ObservableCollection<string>(
            labos.Select(l => l.itemname)
                 .Where(n => !string.IsNullOrWhiteSpace(n))
                 .Distinct()
                 .OrderBy(n => n));
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
            case MonographFilterMode.WithMonograph:
                if (m.monogat == null || m.monogat == DateTime.MinValue)
                    return false;
                break;

            case MonographFilterMode.WithoutMonograph:
                if (m.monogat != null && m.monogat != DateTime.MinValue)
                    return false;
                break;

            case MonographFilterMode.WithoutVoie:
                if (!string.IsNullOrWhiteSpace(m.voie))
                    return false;
                break;

            case MonographFilterMode.WithoutFormGroup:
                if (!string.IsNullOrWhiteSpace(m.formgroup))
                    return false;
                break;
        }

        // Search text
        if (string.IsNullOrWhiteSpace(SearchText)) return true;
        var term = SearchText.Trim();
        return m.pctcode.Contains(term, StringComparison.OrdinalIgnoreCase)
            || m.itemname.Contains(term, StringComparison.OrdinalIgnoreCase)
            || m.dci.Contains(term, StringComparison.OrdinalIgnoreCase)
            || m.labo.Contains(term, StringComparison.OrdinalIgnoreCase)
            || m.voie.Contains(term, StringComparison.OrdinalIgnoreCase)
            || m.forme.Contains(term, StringComparison.OrdinalIgnoreCase);
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

    private void ApplyFilter(MonographFilterMode mode, string label)
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
        ApplyFilter(MonographFilterMode.All, "Tout afficher");
    }

    /// <summary>Afficher les médicaments avec date de monographie.</summary>
    [RelayCommand]
    private void ShowWithMonograph()
    {
        ApplyFilter(MonographFilterMode.WithMonograph, "Avec monographie");
    }

    /// <summary>Afficher les médicaments sans date de monographie.</summary>
    [RelayCommand]
    private void ShowWithoutMonograph()
    {
        ApplyFilter(MonographFilterMode.WithoutMonograph, "Sans monographie");
    }

    /// <summary>Afficher les médicaments sans voie d'administration.</summary>
    [RelayCommand]
    private void ShowWithoutVoie()
    {
        ApplyFilter(MonographFilterMode.WithoutVoie, "Sans voie d'admin.");
    }

    /// <summary>Afficher les médicaments sans groupe de formes.</summary>
    [RelayCommand]
    private void ShowWithoutFormGroup()
    {
        ApplyFilter(MonographFilterMode.WithoutFormGroup, "Sans groupe de formes");
    }

    // ============================================
    // TOOLBAR — ACTION COMMANDS
    // ============================================

    /// <summary>
    /// Définir la date de monographie à aujourd'hui pour tous les
    /// médicaments affichés qui n'en ont pas encore.
    /// </summary>
    [RelayCommand]
    private async Task SetMonographDateAsync()
    {
        var confirmed = await _dialogService.ShowConfirmAsync(
            "Date monographie",
            "Définir la date de monographie à aujourd'hui pour tous les médicaments\n" +
            "affichés qui n'en ont pas encore ?");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            int count = 0;
            var visible = MedicsView?.Cast<Medic>().ToList() ?? _allMedics.ToList();
            var now = DateTime.Now;

            foreach (var m in visible)
            {
                if (m.monogat == null || m.monogat == DateTime.MinValue)
                {
                    m.monogat = now;
                    count++;
                }
            }

            HasUnsavedChanges = count > 0 || HasUnsavedChanges;
            MedicsView?.Refresh();

            await _dialogService.ShowInfoAsync(
                "Date monographie",
                $"{count} date(s) de monographie définie(s) à aujourd'hui.\n" +
                "Cliquez 'Appliquer' pour sauvegarder.");
        }, "Mise à jour des dates...");
    }

    /// <summary>
    /// Effacer les dates de monographie de tous les médicaments affichés.
    /// </summary>
    [RelayCommand]
    private async Task ClearMonographDatesAsync()
    {
        var confirmed = await _dialogService.ShowConfirmAsync(
            "Effacer dates",
            "Effacer les dates de monographie de TOUS les médicaments affichés ?\n" +
            "Les modifications devront être sauvegardées avec 'Appliquer'.");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            int count = 0;
            var visible = MedicsView?.Cast<Medic>().ToList() ?? _allMedics.ToList();

            foreach (var m in visible)
            {
                if (m.monogat != null && m.monogat != DateTime.MinValue)
                {
                    m.monogat = null;
                    count++;
                }
            }

            HasUnsavedChanges = count > 0 || HasUnsavedChanges;
            MedicsView?.Refresh();

            await _dialogService.ShowInfoAsync(
                "Dates effacées",
                $"{count} date(s) de monographie effacée(s).\n" +
                "Cliquez 'Appliquer' pour sauvegarder.");
        }, "Effacement en cours...");
    }

    /// <summary>
    /// Restaurer tout — reload from DB, discarding unsaved changes.
    /// </summary>
    [RelayCommand]
    private async Task RestoreAllAsync()
    {
        if (HasUnsavedChanges)
        {
            var confirmed = await _dialogService.ShowConfirmAsync(
                "Restauration",
                "Des modifications non sauvegardées seront perdues.\nVoulez-vous continuer ?");

            if (!confirmed) return;
        }

        await LoadDataAsync();
        HasUnsavedChanges = false;
    }

    /// <summary>
    /// Appliquer — save all modified medics to the database.
    /// </summary>
    [RelayCommand]
    private async Task ApplyMonographiesAsync()
    {
        if (!HasUnsavedChanges)
        {
            await _dialogService.ShowInfoAsync("Information", "Aucune modification à sauvegarder.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmAsync(
            "Sauvegarder",
            "Voulez-vous appliquer toutes les modifications de monographie à la base de données ?");

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
    // CLEANUP
    // ============================================

    public override void Dispose()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        base.Dispose();
    }

    // ============================================
    // INNER ENUM
    // ============================================

    private enum MonographFilterMode
    {
        All,
        WithMonograph,
        WithoutMonograph,
        WithoutVoie,
        WithoutFormGroup
    }
}
