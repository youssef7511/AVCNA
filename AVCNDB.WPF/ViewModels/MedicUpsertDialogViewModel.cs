using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Helpers;
using AVCNDB.WPF.Messages;
using AVCNDB.WPF.Models;
using AVCNDB.WPF.Services;

namespace AVCNDB.WPF.ViewModels;

public partial class MedicUpsertDialogViewModel : ViewModelBase, INotifyDataErrorInfo
{
    // ── INotifyDataErrorInfo plumbing ──
    private bool _isInitializing;
    private readonly Dictionary<string, List<string>> _fieldErrors = new();
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public bool HasErrors => _fieldErrors.Count > 0;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) return Array.Empty<string>();
        return _fieldErrors.TryGetValue(propertyName, out var list) ? list : (IEnumerable)Array.Empty<string>();
    }

    /// <summary>Aggregated error messages for display in a single panel.</summary>
    [ObservableProperty]
    private ObservableCollection<string> _validationMessages = new();

    private void SetError(string propertyName, string? error)
    {
        if (error == null)
        {
            if (_fieldErrors.Remove(propertyName))
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
                RebuildValidationMessages();
            }
        }
        else
        {
            _fieldErrors[propertyName] = new List<string> { error };
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            RebuildValidationMessages();
        }
        OnPropertyChanged(nameof(HasErrors));
        SaveCommand.NotifyCanExecuteChanged();
    }

    private void RebuildValidationMessages()
    {
        ValidationMessages = new ObservableCollection<string>(
            _fieldErrors.SelectMany(kv => kv.Value));
    }

    /// <summary>Run all field validations. Called before save and after Medic loads.</summary>
    private void ValidateAll()
    {
        if (Medic == null) return;
        SetError(nameof(Medic.itemname),  ValidationRules.Required(Medic.itemname, "Nom du médicament")
                                       ?? ValidationRules.MaxLength(Medic.itemname, 150, "Nom du médicament"));
        SetError(nameof(Medic.barcode),   ValidationRules.Barcode13(Medic.barcode));
        SetError(nameof(Medic.pctcode),   ValidationRules.Required(Medic.pctcode, "Code PCT")
                                       ?? ValidationRules.MaxLength(Medic.pctcode, 20, "Code PCT"));
        SetError(nameof(Medic.amm),       ValidationRules.MaxLength(Medic.amm, 30, "N° AMM"));
        // DCI is optional — no Required check on dci1
        // Famille fields are all optional and clearable. Cascade-order rules
        // below still enforce consistency (fam2 set requires fam1 set, etc.).
        SetError(nameof(Medic.fam1),      null);
        // Famille order: fam2 requires fam1, fam3 requires fam2, family requires fam1
        SetError("fam2Order", !string.IsNullOrWhiteSpace(Medic.fam2) && string.IsNullOrWhiteSpace(Medic.fam1)
            ? "Famille 2 ne peut pas être renseignée sans Famille 1." : null);
        SetError("fam3Order", !string.IsNullOrWhiteSpace(Medic.fam3) && string.IsNullOrWhiteSpace(Medic.fam2)
            ? "Famille 3 ne peut pas être renseignée sans Famille 2." : null);
        SetError("familyOrder", !string.IsNullOrWhiteSpace(Medic.family) && string.IsNullOrWhiteSpace(Medic.fam1)
            ? "Famille 4 ne peut pas être renseignée sans Famille 1." : null);
        SetError(nameof(Medic.price),     ValidationRules.NonNegative(Medic.price, "Prix Fab. HT"));
        SetError(nameof(Medic.refprice),  ValidationRules.NonNegative(Medic.refprice, "Prix Hospitalier"));
        SetError(nameof(Medic.pamount),   ValidationRules.NonNegative(Medic.pamount, "PPV"));
        SetError(nameof(Medic.pctprice),  ValidationRules.NonNegative(Medic.pctprice, "Prix de Gros"));
        SetError(nameof(Medic.netprice),  ValidationRules.NonNegative(Medic.netprice, "Prix Base Remb."));
        SetError(nameof(Medic.timbrepct), ValidationRules.NonNegative(Medic.timbrepct, "Timbre"));
        SetError(nameof(Medic.colisage),  ValidationRules.NonNegative(Medic.colisage, "Colisage"));
        SetError(nameof(Medic.ictx),      ValidationRules.PercentRange(Medic.ictx));
        // Cross-field
        SetError("RefVsPpv",              ValidationRules.RefPriceNotAbovePpv(Medic.refprice, Medic.pamount));
    }

    // Famille fill-state helpers — used by XAML IsEnabled bindings
    public bool IsFam1Filled => !string.IsNullOrWhiteSpace(Medic?.fam1);
    public bool IsFam2Filled => !string.IsNullOrWhiteSpace(Medic?.fam2);
    public bool IsFam3Filled => !string.IsNullOrWhiteSpace(Medic?.fam3);

    // ── DCI/Dose/Unit proxies — wired to refresh the summary band on each keystroke ──
    public string Dci1
    {
        get => Medic?.dci1 ?? string.Empty;
        set { if (Medic == null || Medic.dci1 == value) return; Medic.dci1 = value; OnPropertyChanged(); RefreshComputedDciSummary(); }
    }
    public string Dose1
    {
        get => Medic?.dose1 ?? string.Empty;
        set { if (Medic == null || Medic.dose1 == value) return; Medic.dose1 = value; OnPropertyChanged(); RefreshComputedDciSummary(); }
    }
    public string U1
    {
        get => Medic?.u1 ?? string.Empty;
        set { if (Medic == null || Medic.u1 == value) return; Medic.u1 = value; OnPropertyChanged(); RefreshComputedDciSummary(); }
    }
    public string Dci2
    {
        get => Medic?.dci2 ?? string.Empty;
        set { if (Medic == null || Medic.dci2 == value) return; Medic.dci2 = value; OnPropertyChanged(); RefreshComputedDciSummary(); }
    }
    public string Dose2
    {
        get => Medic?.dose2 ?? string.Empty;
        set { if (Medic == null || Medic.dose2 == value) return; Medic.dose2 = value; OnPropertyChanged(); RefreshComputedDciSummary(); }
    }
    public string U2
    {
        get => Medic?.u2 ?? string.Empty;
        set { if (Medic == null || Medic.u2 == value) return; Medic.u2 = value; OnPropertyChanged(); RefreshComputedDciSummary(); }
    }
    public string Dci3
    {
        get => Medic?.dci3 ?? string.Empty;
        set { if (Medic == null || Medic.dci3 == value) return; Medic.dci3 = value; OnPropertyChanged(); RefreshComputedDciSummary(); }
    }
    public string Dose3
    {
        get => Medic?.dose3 ?? string.Empty;
        set { if (Medic == null || Medic.dose3 == value) return; Medic.dose3 = value; OnPropertyChanged(); RefreshComputedDciSummary(); }
    }
    public string U3
    {
        get => Medic?.u3 ?? string.Empty;
        set { if (Medic == null || Medic.u3 == value) return; Medic.u3 = value; OnPropertyChanged(); RefreshComputedDciSummary(); }
    }
    public string Dci4
    {
        get => Medic?.dci4 ?? string.Empty;
        set { if (Medic == null || Medic.dci4 == value) return; Medic.dci4 = value; OnPropertyChanged(); RefreshComputedDciSummary(); }
    }
    public string Dose4
    {
        get => Medic?.dose4 ?? string.Empty;
        set { if (Medic == null || Medic.dose4 == value) return; Medic.dose4 = value; OnPropertyChanged(); RefreshComputedDciSummary(); }
    }
    public string U4
    {
        get => Medic?.u4 ?? string.Empty;
        set { if (Medic == null || Medic.u4 == value) return; Medic.u4 = value; OnPropertyChanged(); RefreshComputedDciSummary(); }
    }

    partial void OnMedicChanged(Medic value)
    {
        // Proxy/computed notifications MUST fire even during init so the
        // FilterableComboBox bindings (DCI1..4, Dose1..4, U1..4) pick up
        // the loaded Medic's values. Without these, DCI combos open empty
        // on existing drugs. Only ValidateAll is skipped during init.
        RefreshComputedDciSummary();
        OnPropertyChanged(nameof(IsFam1Filled));
        OnPropertyChanged(nameof(IsFam2Filled));
        OnPropertyChanged(nameof(IsFam3Filled));
        OnPropertyChanged(nameof(Dci1));
        OnPropertyChanged(nameof(Dose1));
        OnPropertyChanged(nameof(U1));
        OnPropertyChanged(nameof(Dci2));
        OnPropertyChanged(nameof(Dose2));
        OnPropertyChanged(nameof(U2));
        OnPropertyChanged(nameof(Dci3));
        OnPropertyChanged(nameof(Dose3));
        OnPropertyChanged(nameof(U3));
        OnPropertyChanged(nameof(Dci4));
        OnPropertyChanged(nameof(Dose4));
        OnPropertyChanged(nameof(U4));

        if (_isInitializing) return;
        ValidateAll();
    }

    partial void OnSelectedCompareDrugChanged(Medic? value)
    {
        CompareInteractions = new();
        DeepAnalysisHasPendingResult = false;
        if (value != null)
            _ = LoadCompareInteractionsAsync(value);
        RunDeepAnalysisCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsDeepAnalysisRunningChanged(bool value)
    {
        RunDeepAnalysisCommand.NotifyCanExecuteChanged();
    }

    private readonly IRepository<Medic> _repository;
    private readonly IRepository<Families> _familyRepository;
    private readonly IRepository<Labos> _laboRepository;
    private readonly IRepository<Dci> _dciRepository;
    private readonly IRepository<Formes> _formeRepository;
    private readonly IRepository<Presents> _presentRepository;
    private readonly IRepository<Voies> _voieRepository;
    private readonly IRepository<Interact> _interactRepository;
    private readonly IOpenRouterService _openRouterService;
    private readonly IMLPfeService _mlPfeService;
    private readonly IDialogService _dialogService;
    private readonly MedicSyncService _syncService;
    private CancellationTokenSource? _analysisCts;

    // ── Dirty tracking ──
    private string _originalMedicSnapshot = string.Empty;

    /// <summary>
    /// True when any field differs from the state captured at load time.
    /// Used by Cancel and OnClosing to decide whether to show a discard warning.
    /// </summary>
    public bool HasUnsavedChanges =>
        !SavedSuccessfully &&
        !string.IsNullOrEmpty(_originalMedicSnapshot) &&
        SnapFields(Medic) != _originalMedicSnapshot;

    private void TakeSnapshot() =>
        _originalMedicSnapshot = SnapFields(Medic);

    /// <summary>All user-editable Medic fields joined into a single comparable string.</summary>
    private static string SnapFields(Medic m) => string.Join("\x00",
        m.itemname, m.basename, m.shortname, m.barcode, m.pctcode, m.amm,
        m.dci1, m.dose1, m.u1, m.dci2, m.dose2, m.u2,
        m.dci3, m.dose3, m.u3, m.dci4, m.dose4, m.u4,
        m.fam1, m.fam2, m.fam3, m.family,
        m.labo, m.specialite, m.veic,
        m.forme, m.voie, m.present, m.colisage, m.ucol,
        m.price, m.refprice, m.pamount, m.pctprice, m.netprice, m.timbrepct,
        m.indication, m.mgarde, m.posology, m.formgroup,
        m.ictx, m.tableau, m.pediatric, m.isap, m.isic, m.isotc);

    public Task<bool> ConfirmDiscardAsync() =>
        _dialogService.ShowConfirmAsync(
            "Modifications non sauvegardées",
            "Des modifications non sauvegardées seront perdues.\nFermer quand même sans enregistrer ?");

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string _pageTitle = "Nouveau Médicament";

    [ObservableProperty]
    private Medic _medic = new();

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _computedDenomination = string.Empty;

    [ObservableProperty]
    private string _computedDciSummary = string.Empty;

    [ObservableProperty]
    private string _computedPosology = string.Empty;

    // ── Posology builder fields ──
    [ObservableProperty]
    private decimal _posoQty;

    [ObservableProperty]
    private string _posoForm = string.Empty;

    [ObservableProperty]
    private decimal _posoPrises;

    [ObservableProperty]
    private string _posoPeriode = string.Empty;

    [ObservableProperty]
    private string _posoConditions = string.Empty;

    // ── Mode C: Interactions for this drug ──
    [ObservableProperty]
    private ObservableCollection<Interact> _medicInteractions = new();

    // ── Mode A: Compare with another drug ──
    [ObservableProperty]
    private string _compareSearchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Medic> _compareSearchResults = new();

    [ObservableProperty]
    private Medic? _selectedCompareDrug;

    [ObservableProperty]
    private ObservableCollection<Interact> _compareInteractions = new();

    [ObservableProperty]
    private bool _isCompareSearchRunning;

    // ── Tier 3: Deep Analysis (OpenRouter) ──
    [ObservableProperty]
    private bool _isDeepAnalysisRunning;

    [ObservableProperty]
    private bool _deepAnalysisHasPendingResult;

    [ObservableProperty]
    private string _deepAnalysisLevel = string.Empty;

    [ObservableProperty]
    private string _deepAnalysisDescription = string.Empty;

    [ObservableProperty]
    private string _deepAnalysisMecanisme = string.Empty;

    [ObservableProperty]
    private string _deepAnalysisConduite = string.Empty;

    /// <summary>Single-row collection for the Dénomination tab DataGrid (mirrors DenominationView columns).</summary>
    [ObservableProperty]
    private ObservableCollection<Medic> _denominationRow = new();

    // ── Lookup collections ──
    [ObservableProperty]
    private ObservableCollection<Families> _families = new();

    [ObservableProperty]
    private ObservableCollection<Labos> _labos = new();

    [ObservableProperty]
    private ObservableCollection<Dci> _dcis = new();

    [ObservableProperty]
    private ObservableCollection<Formes> _formes = new();

    [ObservableProperty]
    private ObservableCollection<Presents> _presents = new();

    [ObservableProperty]
    private ObservableCollection<Voies> _voies = new();

    /// <summary>Set to true when Save completes successfully; the dialog reads this to decide DialogResult.</summary>
    public bool SavedSuccessfully { get; private set; }

    public MedicUpsertDialogViewModel(
        IRepository<Medic> repository,
        IRepository<Families> familyRepository,
        IRepository<Labos> laboRepository,
        IRepository<Dci> dciRepository,
        IRepository<Formes> formeRepository,
        IRepository<Presents> presentRepository,
        IRepository<Voies> voieRepository,
        IRepository<Interact> interactRepository,
        IOpenRouterService openRouterService,
        IMLPfeService mlPfeService,
        IDialogService dialogService,
        MedicSyncService syncService)
    {
        _repository = repository;
        _familyRepository = familyRepository;
        _laboRepository = laboRepository;
        _dciRepository = dciRepository;
        _formeRepository = formeRepository;
        _presentRepository = presentRepository;
        _voieRepository = voieRepository;
        _interactRepository = interactRepository;
        _openRouterService = openRouterService;
        _mlPfeService = mlPfeService;
        _dialogService = dialogService;
        _syncService = syncService;
    }

    [RelayCommand]
    private async Task LaunchMlPfeAsync()
    {
        var error = await _mlPfeService.LaunchAndOpenAsync();
        if (error != null)
            await _dialogService.ShowErrorAsync("IA · Contre-indications patient", error);
    }

    /// <summary>
    /// Initialise the dialog for a new or existing medic.
    /// Called by the dialog code-behind after construction.
    /// </summary>
    public async Task InitializeAsync(int? medicId)
    {
        _isInitializing = true;
        try
        {
            await LoadReferenceDataAsync();

            if (medicId.HasValue)
            {
                IsEditMode = true;
                PageTitle = "Modifier le Médicament";
                await LoadMedicAsync(medicId.Value);
                await LoadInteractionsAsync();
            }
            else
            {
                IsEditMode = false;
                PageTitle = "Nouveau Médicament";
                Medic = new Medic { isactive = 1 };
            }

            // Snapshot must be taken AFTER the medic is fully loaded so that
            // HasUnsavedChanges only fires when the user actually edits something.
            TakeSnapshot();

            DenominationRow = new ObservableCollection<Medic> { Medic };
            RefreshComputedDenomination();
            RefreshComputedDciSummary();
        }
        finally
        {
            _isInitializing = false;
            // Clear any validation state accumulated during init — form starts clean.
            _fieldErrors.Clear();
            RebuildValidationMessages();
            OnPropertyChanged(nameof(HasErrors));
            SaveCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task LoadReferenceDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            var families = await _familyRepository.GetAllAsync();
            Families = new ObservableCollection<Families>(families);

            var labos = await _laboRepository.GetAllAsync();
            Labos = new ObservableCollection<Labos>(labos);

            var dcis = await _dciRepository.GetAllAsync();
            Dcis = new ObservableCollection<Dci>(dcis);

            var formes = await _formeRepository.GetAllAsync();
            Formes = new ObservableCollection<Formes>(formes);

            var presents = await _presentRepository.GetAllAsync();
            Presents = new ObservableCollection<Presents>(presents);

            var voies = await _voieRepository.GetAllAsync();
            Voies = new ObservableCollection<Voies>(voies);
        }, "Chargement des données de référence...");
    }

    private async Task LoadMedicAsync(int medicId)
    {
        await ExecuteAsync(async () =>
        {
            var medic = await _repository.GetByIdAsync(medicId);
            if (medic != null)
            {
                Medic = medic;
            }
        }, "Chargement du médicament...");
    }

    private async Task LoadInteractionsAsync()
    {
        var dcis = new[] { Medic.dci1, Medic.dci2, Medic.dci3, Medic.dci4 }
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (dcis.Count == 0) return;

        var voie = Medic.voie?.Trim() ?? string.Empty;
        var all = await _interactRepository.FindAsync(i =>
            (dcis.Contains(i.dci1) && (string.IsNullOrEmpty(i.voie1) || i.voie1 == voie)) ||
            (dcis.Contains(i.dci2) && (string.IsNullOrEmpty(i.voie2) || i.voie2 == voie)));

        MedicInteractions = new ObservableCollection<Interact>(all);
    }

    // ── Denomination builder ──

    [RelayCommand]
    private void UpdateDenomination()
    {
        Medic.basename = MedicDenominationHelper.BuildDenomination(Medic);
        RefreshComputedDenomination();
    }

    public void RefreshComputedDenomination()
    {
        ComputedDenomination = MedicDenominationHelper.BuildDenomination(Medic);
        RefreshComputedDciSummary();
    }

    public void RefreshComputedDciSummary()
    {
        if (Medic == null) { ComputedDciSummary = string.Empty; return; }
        var parts = new[]
        {
            BuildDciPart(Medic.dci1, Medic.dose1, Medic.u1),
            BuildDciPart(Medic.dci2, Medic.dose2, Medic.u2),
            BuildDciPart(Medic.dci3, Medic.dose3, Medic.u3),
            BuildDciPart(Medic.dci4, Medic.dose4, Medic.u4),
        }.Where(p => !string.IsNullOrWhiteSpace(p));
        ComputedDciSummary = string.Join(" + ", parts);
    }

    private static string? BuildDciPart(string? dci, string? dose, string? unit)
    {
        if (string.IsNullOrWhiteSpace(dci)) return null;
        var suffix = $"{dose?.Trim()} {unit?.Trim()}".Trim();
        return string.IsNullOrWhiteSpace(suffix) ? dci.Trim() : $"{dci.Trim()} {suffix}";
    }

    // ── Posology builder ──

    [RelayCommand]
    private void UpdatePosology()
    {
        var parts = new List<string>();

        if (PosoQty > 0) parts.Add($"{PosoQty:G29}");
        if (!string.IsNullOrWhiteSpace(PosoForm)) parts.Add(PosoForm.Trim());
        if (PosoPrises > 0) parts.Add($"{PosoPrises:G29} fois");
        if (!string.IsNullOrWhiteSpace(PosoPeriode)) parts.Add(PosoPeriode.Trim());
        if (!string.IsNullOrWhiteSpace(PosoConditions)) parts.Add($"({PosoConditions.Trim()})");

        var posology = string.Join(" ", parts);
        Medic.posology = posology;
        ComputedPosology = posology;
    }

    // ── Validation ──

    private bool Validate()
    {
        ValidateAll();
        HasError = HasErrors;
        if (HasErrors)
        {
            ErrorMessage = string.Join(" • ", _fieldErrors.SelectMany(kv => kv.Value));
            return false;
        }
        ErrorMessage = null;
        return true;
    }

    private bool CanSave() => !HasErrors;

    // ── Save ──

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (!Validate())
        {
            await _dialogService.ShowWarningAsync("Validation",
                ErrorMessage ?? "Veuillez corriger les erreurs avant de sauvegarder.");
            return;
        }

        // Confirmation popup — required for security on every edit/create
        var confirmTitle = IsEditMode ? "Confirmer la modification" : "Confirmer la création";
        var confirmMsg = IsEditMode
            ? $"Enregistrer les modifications apportées à\n« {Medic.itemname} » ?"
            : $"Créer le nouveau médicament\n« {Medic.itemname} » ?";
        var confirmed = await _dialogService.ShowConfirmAsync(confirmTitle, confirmMsg);
        if (!confirmed) return;

        try
        {
            // Rebuild combined DCI display field from individual dci1..dci4
            Medic.dci = string.Join(" + ", new[] { Medic.dci1, Medic.dci2, Medic.dci3, Medic.dci4 }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim()));

            // Set timestamps
            var now = DateTime.Now;
            if (IsEditMode)
            {
                Medic.updatedat = now;
                await _repository.UpdateAsync(Medic);
            }
            else
            {
                Medic.addedat = now;
                await _repository.AddAsync(Medic);
            }

            // Sync lookup tables (non-fatal)
            try { await _syncService.SyncLookupTablesAsync(Medic); } catch { /* non-fatal */ }

            // Notify other ViewModels
            WeakReferenceMessenger.Default.Send(new DataChangedMessage(
                new DataChangeInfo("Medic",
                    IsEditMode ? ChangeOperation.Updated : ChangeOperation.Created,
                    Medic.recordid)));

            SavedSuccessfully = true;

            await _dialogService.ShowSuccessAsync("Succès",
                IsEditMode ? "Médicament mis à jour avec succès." : "Médicament créé avec succès.");

            // Close the dialog — the code-behind listens for SavedSuccessfully
            RequestClose?.Invoke(true);
        }
        catch (Exception ex)
        {
            var innermost = ex;
            while (innermost.InnerException != null)
                innermost = innermost.InnerException;

            HasError = true;
            ErrorMessage = innermost.Message;
            await _dialogService.ShowErrorAsync("Erreur de sauvegarde", innermost.Message);
        }
    }

    // ── Cancel ──

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (HasUnsavedChanges)
        {
            var confirm = await _dialogService.ShowConfirmAsync(
                "Modifications non sauvegardées",
                "Des modifications non sauvegardées seront perdues.\nAnnuler quand même ?");

            if (!confirm) return;
        }

        RequestClose?.Invoke(false);
    }

    /// <summary>
    /// The dialog code-behind subscribes to this to close the Window.
    /// Parameter: true = saved, false = cancelled.
    /// </summary>
    public event Action<bool>? RequestClose;

    // ── Mode A: Compare Drug Search ──

    [RelayCommand]
    private async Task SearchCompareDrugAsync()
    {
        var text = CompareSearchText.Trim();
        if (string.IsNullOrEmpty(text)) return;

        IsCompareSearchRunning = true;
        try
        {
            var results = await _repository.FindAsync(m => m.itemname.Contains(text));
            CompareSearchResults = new ObservableCollection<Medic>(results.Take(15));
        }
        finally
        {
            IsCompareSearchRunning = false;
        }
    }

    private async Task LoadCompareInteractionsAsync(Medic compareDrug)
    {
        var myDcis = new[] { Medic.dci1, Medic.dci2, Medic.dci3, Medic.dci4 }
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Select(d => d!.Trim())
            .ToList();

        var theirDcis = new[] { compareDrug.dci1, compareDrug.dci2, compareDrug.dci3, compareDrug.dci4 }
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Select(d => d!.Trim())
            .ToList();

        if (myDcis.Count == 0 || theirDcis.Count == 0)
        {
            CompareInteractions = new();
            return;
        }

        var results = await _interactRepository.FindAsync(i =>
            (myDcis.Contains(i.dci1) && theirDcis.Contains(i.dci2)) ||
            (myDcis.Contains(i.dci2) && theirDcis.Contains(i.dci1)));

        CompareInteractions = new ObservableCollection<Interact>(results);
    }

    // ── Tier 3: Deep Analysis (OpenRouter) ──

    [RelayCommand(CanExecute = nameof(CanRunDeepAnalysis))]
    private async Task RunDeepAnalysisAsync()
    {
        if (SelectedCompareDrug == null || string.IsNullOrWhiteSpace(Medic?.dci1)) return;

        _analysisCts?.Cancel();
        _analysisCts = new CancellationTokenSource();

        IsDeepAnalysisRunning = true;
        DeepAnalysisHasPendingResult = false;

        try
        {
            var analysis = await _openRouterService.AnalyzeInteractionAsync(
                Medic.dci1.Trim(), Medic.voie?.Trim() ?? string.Empty,
                SelectedCompareDrug.dci1.Trim(), SelectedCompareDrug.voie?.Trim() ?? string.Empty,
                _analysisCts.Token);

            DeepAnalysisLevel       = analysis.Level;
            DeepAnalysisDescription = analysis.Description;
            DeepAnalysisMecanisme   = analysis.Mecanisme;
            DeepAnalysisConduite    = analysis.Conduite;
            DeepAnalysisHasPendingResult = true;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Analyse IA", $"Erreur OpenRouter : {ex.Message}");
        }
        finally
        {
            IsDeepAnalysisRunning = false;
        }
    }

    private bool CanRunDeepAnalysis() =>
        SelectedCompareDrug != null &&
        !string.IsNullOrWhiteSpace(Medic?.dci1) &&
        !IsDeepAnalysisRunning;

    [RelayCommand]
    private async Task ApproveAndSaveInteractionAsync()
    {
        if (!DeepAnalysisHasPendingResult || SelectedCompareDrug == null) return;

        var confirmed = await _dialogService.ShowConfirmAsync(
            "Enregistrer l'interaction",
            $"Sauvegarder l'interaction IA entre '{Medic.dci1}' et '{SelectedCompareDrug.dci1}' ?\n\nNiveau : {DeepAnalysisLevel}");

        if (!confirmed) return;

        try
        {
            var now = DateTime.Now;
            var interact = new Interact
            {
                dci1        = Medic.dci1.Trim(),
                dci2        = SelectedCompareDrug.dci1.Trim(),
                voie1       = Medic.voie?.Trim() ?? string.Empty,
                voie2       = SelectedCompareDrug.voie?.Trim() ?? string.Empty,
                level       = Cap(DeepAnalysisLevel,       LevelMaxLength),
                description = Cap(DeepAnalysisDescription, DescriptionMaxLength),
                mecanisme   = Cap(DeepAnalysisMecanisme,   MecanismeMaxLength),
                conduite    = Cap(DeepAnalysisConduite,    ConduiteMaxLength),
                addedat     = now,
                updatedat   = now
            };

            await _interactRepository.AddAsync(interact);
            DeepAnalysisHasPendingResult = false;

            // Refresh both grids to reflect the new entry
            await LoadInteractionsAsync();
            await LoadCompareInteractionsAsync(SelectedCompareDrug);

            await _dialogService.ShowSuccessAsync("Succès", "Interaction enregistrée dans la base de données.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Erreur", FlattenError(ex));
        }
    }

    [RelayCommand]
    private void DiscardDeepAnalysis()
    {
        _analysisCts?.Cancel();
        DeepAnalysisHasPendingResult = false;
        DeepAnalysisLevel = DeepAnalysisDescription = DeepAnalysisMecanisme = DeepAnalysisConduite = string.Empty;
    }

    private static string FlattenError(Exception ex)
    {
        var sb = new System.Text.StringBuilder();
        var current = ex;
        while (current != null)
        {
            if (sb.Length > 0) sb.Append("\n→ ");
            sb.Append(current.Message);
            current = current.InnerException;
        }
        return sb.ToString();
    }

    // Column bounds mirror StringLength attributes on Models/Interact.cs.
    private const int LevelMaxLength       = 20;
    private const int DescriptionMaxLength = 500;
    private const int MecanismeMaxLength   = 200;
    private const int ConduiteMaxLength    = 500;

    private static string Cap(string? value, int max)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= max ? value : value.Substring(0, max - 1) + "…";
    }

    // INavigationAware — not used in dialog mode
    public override void OnNavigatedTo(object? parameter) { }
    public override void OnNavigatedFrom() { }
}
