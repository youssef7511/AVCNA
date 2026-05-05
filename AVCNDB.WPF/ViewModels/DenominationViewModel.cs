using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Helpers;
using AVCNDB.WPF.Messages;
using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.ViewModels;

/// <summary>
/// ViewModel pour la gestion et l'édition collective des dénominations.
/// Charge TOUS les médicaments dans un DataGrid éditable avec recherche/filtre.
/// </summary>
public partial class DenominationViewModel : ViewModelBase
{
    private readonly IRepository<Medic> _repository;
    private readonly IRepository<Formes> _formesRepository;
    private readonly IRepository<Presents> _presentsRepository;
    private readonly IRepository<Labos> _labosRepository;
    private readonly IDialogService _dialogService;

    private ObservableCollection<Medic> _allMedics = new();

    /// <summary>Column headers that contribute to the denomination auto-computation.</summary>
    private static readonly HashSet<string> _denominationFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "Dose 1", "U1", "Dose 2", "U2", "Dose 3", "U3", "Dose 4", "U4",
        "Forme", "Présent.", "Colisage", "Unités"
    };

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

    /// <summary>
    /// Computed denomination for the selected medic.
    /// Built from: basename + dose1 + u1 + forme + present + colisage + ucol
    /// </summary>
    [ObservableProperty]
    private string _computedDenomination = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _formesList = new();

    [ObservableProperty]
    private ObservableCollection<string> _presentsList = new();

    [ObservableProperty]
    private ObservableCollection<string> _labosList = new();

    public DenominationViewModel(
        IRepository<Medic> repository,
        IRepository<Formes> formesRepository,
        IRepository<Presents> presentsRepository,
        IRepository<Labos> labosRepository,
        IDialogService dialogService)
    {
        _repository = repository;
        _formesRepository = formesRepository;
        _presentsRepository = presentsRepository;
        _labosRepository = labosRepository;
        _dialogService = dialogService;

        // Refresh ComboBox lists when library data changes
        WeakReferenceMessenger.Default.Register<DataChangedMessage>(this, (r, m) =>
        {
            var entityType = m.Value.EntityType;
            if (entityType is "Formes" or "Presents" or "Labos")
            {
                App.Current.Dispatcher.InvokeAsync(async () => await LoadLibraryListsAsync());
            }
        });

        _ = LoadDataAsync();
    }

    /// <summary>
    /// Called when navigated to — data already loaded in constructor.
    /// </summary>
    public override void OnNavigatedTo(object? parameter)
    {
        // Data already loaded in constructor; no-op.
    }

    /// <summary>
    /// Loads all medics from the database (no pagination).
    /// </summary>
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
            MedicsView.CollectionChanged += (_, _) => UpdateFilteredCount();
            UpdateFilteredCount();
        }, "Chargement des dénominations...");
    }

    /// <summary>
    /// Loads FormesList, PresentsList, LabosList from their respective repositories.
    /// </summary>
    private async Task LoadLibraryListsAsync()
    {
        var formes = await _formesRepository.GetAllAsync();
        FormesList = new ObservableCollection<string>(
            formes.Select(f => f.itemname)
                  .Where(n => !string.IsNullOrWhiteSpace(n))
                  .Distinct()
                  .OrderBy(n => n));

        var presents = await _presentsRepository.GetAllAsync();
        PresentsList = new ObservableCollection<string>(
            presents.Select(p => p.itemname)
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

    /// <summary>
    /// Predicate for the ICollectionView filter.
    /// Searches across pctcode, itemname, basename, dci, labo.
    /// </summary>
    private bool FilterMedic(object obj)
    {
        if (obj is not Medic m) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var term = SearchText.Trim();
        return m.pctcode.Contains(term, StringComparison.OrdinalIgnoreCase)
            || m.itemname.Contains(term, StringComparison.OrdinalIgnoreCase)
            || m.basename.Contains(term, StringComparison.OrdinalIgnoreCase)
            || m.dci.Contains(term, StringComparison.OrdinalIgnoreCase)
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

    partial void OnSelectedMedicChanged(Medic? value)
    {
        UpdateComputedDenomination();
    }

    /// <summary>
    /// Called from code-behind when the user finishes editing a DataGrid cell.
    /// When a denomination-contributing field is edited, auto-recomputes
    /// the "Nouvelle Dénomination" (basename) for that row.
    /// </summary>
    public void NotifyCellEdited(Medic? editedMedic, string? columnHeader)
    {
        HasUnsavedChanges = true;

        // Auto-recompute basename when a denomination-contributing field changes
        if (editedMedic != null && columnHeader != null && _denominationFields.Contains(columnHeader))
        {
            editedMedic.basename = BuildDenomination(editedMedic);
            // Deferred refresh so the DataGrid picks up the new basename value
            App.Current.Dispatcher.InvokeAsync(
                () => MedicsView?.Refresh(),
                System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        UpdateComputedDenomination();
    }

    private void UpdateFilteredCount()
    {
        if (MedicsView is ICollectionView cv)
        {
            FilteredCount = cv.Cast<object>().Count();
        }
    }

    /// <summary>
    /// Rebuilds the computed denomination for the currently selected medic.
    /// Formula: basename + dose1 + u1 + forme + present + colisage + ucol
    /// </summary>
    private void UpdateComputedDenomination()
    {
        if (SelectedMedic is null)
        {
            ComputedDenomination = string.Empty;
            return;
        }

        ComputedDenomination = BuildDenomination(SelectedMedic);
    }

    private static string BuildDenomination(Medic m) =>
        MedicDenominationHelper.BuildDenomination(m);

    // ============================================
    // TOOLBAR COMMANDS
    // ============================================

    /// <summary>
    /// Afficher tout — clear search + refresh.
    /// </summary>
    [RelayCommand]
    private void ShowAll()
    {
        SearchText = string.Empty;
        MedicsView?.Refresh();
        UpdateFilteredCount();
    }

    /// <summary>
    /// Initialiser tout — reset basename to empty for ALL medics.
    /// </summary>
    [RelayCommand]
    private async Task InitializeAllAsync()
    {
        var confirmed = await _dialogService.ShowConfirmAsync(
            "Initialisation",
            "Voulez-vous initialiser (vider) les nouvelles dénominations de TOUS les médicaments ?");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            foreach (var m in _allMedics)
            {
                m.basename = string.Empty;
            }

            HasUnsavedChanges = true;
            MedicsView?.Refresh();
            UpdateComputedDenomination();

            await _dialogService.ShowInfoAsync(
                "Initialisation terminée",
                "Toutes les nouvelles dénominations ont été vidées.\nCliquez 'Appliquer' pour sauvegarder.");
        }, "Initialisation en cours...");
    }

    /// <summary>
    /// Restaurer tout — reload from DB, discarding changes.
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
    /// Initialiser les pointeurs — reset dose/unit fields.
    /// </summary>
    [RelayCommand]
    private async Task InitializePointersAsync()
    {
        var confirmed = await _dialogService.ShowConfirmAsync(
            "Initialiser les pointeurs",
            "Voulez-vous réinitialiser les champs dose et unité de TOUS les médicaments ?");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            foreach (var m in _allMedics)
            {
                m.dose1 = string.Empty;
                m.dose2 = string.Empty;
                m.dose3 = string.Empty;
                m.dose4 = string.Empty;
                m.dose5 = string.Empty;
                m.u1 = string.Empty;
                m.u2 = string.Empty;
                m.u3 = string.Empty;
                m.u4 = string.Empty;
                m.u5 = string.Empty;
            }

            HasUnsavedChanges = true;
            MedicsView?.Refresh();
            UpdateComputedDenomination();

            await _dialogService.ShowInfoAsync(
                "Initialisation terminée",
                "Tous les pointeurs (doses/unités) ont été réinitialisés.\nCliquez 'Appliquer' pour sauvegarder.");
        }, "Initialisation des pointeurs...");
    }

    /// <summary>
    /// Appliquer les dénominations:
    /// 1) For each medic where basename is not empty:
    ///    copy basename → itemname, then clear basename.
    /// 2) Save all medics to the DB.
    /// </summary>
    [RelayCommand]
    private async Task ApplyDenominationsAsync()
    {
        if (!HasUnsavedChanges)
        {
            await _dialogService.ShowInfoAsync("Information", "Aucune modification à sauvegarder.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmAsync(
            "Sauvegarder",
            "Voulez-vous appliquer toutes les modifications à la base de données ?\n" +
            "Les 'Nouvelles Dénominations' seront transférées vers 'Ancienne dénomination'.");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            int transferred = 0;
            foreach (var m in _allMedics)
            {
                // Transfer basename → itemname if basename has a value
                if (!string.IsNullOrWhiteSpace(m.basename))
                {
                    m.itemname = m.basename.Trim();
                    m.basename = string.Empty;
                    transferred++;
                }

                await _repository.UpdateAsync(m);
            }

            HasUnsavedChanges = false;
            MedicsView?.Refresh();
            UpdateComputedDenomination();

            await _dialogService.ShowSuccessAsync(
                "Sauvegarde réussie",
                $"{transferred} dénomination(s) transférée(s) vers 'Ancienne dénomination'.\n" +
                $"{_allMedics.Count} médicament(s) sauvegardés.");
        }, "Sauvegarde en cours...");
    }

    public override void Dispose()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        base.Dispose();
    }
}
