using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FuzzySharp;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Helpers;
using AVCNDB.WPF.Messages;
using AVCNDB.WPF.Models;
using AVCNDB.WPF.Services;
using Microsoft.Extensions.DependencyInjection;

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
        // Edit-mode only: critical reference fields cannot be cleared.
        // In create-mode (IsEditMode == false), only itemname + pctcode are required;
        // the other references can be filled in later.
        SetError(nameof(Medic.dci1), IsEditMode
            ? ValidationRules.Required(Medic.dci1, "DCI Principal")
            : null);
        SetError(nameof(Medic.fam1), IsEditMode
            ? ValidationRules.Required(Medic.fam1, "Famille 1")
            : null);
        SetError(nameof(Medic.labo), IsEditMode
            ? ValidationRules.Required(Medic.labo, "Laboratoire")
            : null);
        // Famille order: fam2 requires fam1, fam3 requires fam2, family requires fam1
        SetError("fam2Order", !string.IsNullOrWhiteSpace(Medic.fam2) && string.IsNullOrWhiteSpace(Medic.fam1)
            ? "Famille 2 ne peut pas être renseignée sans Famille 1." : null);
        SetError("fam3Order", !string.IsNullOrWhiteSpace(Medic.fam3) && string.IsNullOrWhiteSpace(Medic.fam2)
            ? "Famille 3 ne peut pas être renseignée sans Famille 2." : null);
        SetError("familyOrder", !string.IsNullOrWhiteSpace(Medic.family) && string.IsNullOrWhiteSpace(Medic.fam1)
            ? "Famille 4 ne peut pas être renseignée sans Famille 1." : null);
        SetError("dci2Order", !string.IsNullOrWhiteSpace(Medic.dci2) && string.IsNullOrWhiteSpace(Medic.dci1)
            ? "DCI 2 ne peut pas être renseignée sans DCI Principal." : null);
        SetError("dci3Order", !string.IsNullOrWhiteSpace(Medic.dci3) && string.IsNullOrWhiteSpace(Medic.dci2)
            ? "DCI 3 ne peut pas être renseignée sans DCI 2." : null);
        SetError("dci4Order", !string.IsNullOrWhiteSpace(Medic.dci4) && string.IsNullOrWhiteSpace(Medic.dci3)
            ? "DCI 4 ne peut pas être renseignée sans DCI 3." : null);
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

    // ── Famille proxies — cascade-clear descendants when a parent is cleared ──
    public string Fam1
    {
        get => Medic?.fam1 ?? string.Empty;
        set
        {
            if (Medic == null || Medic.fam1 == value) return;
            Medic.fam1 = value;
            OnPropertyChanged();
            if (string.IsNullOrWhiteSpace(value))
            {
                Medic.fam2   = string.Empty;
                Medic.fam3   = string.Empty;
                Medic.family = string.Empty;
                OnPropertyChanged(nameof(Fam2));
                OnPropertyChanged(nameof(Fam3));
                OnPropertyChanged(nameof(Family));
            }
            OnPropertyChanged(nameof(IsFam1Filled));
            OnPropertyChanged(nameof(IsFam2Filled));
            OnPropertyChanged(nameof(IsFam3Filled));
            ValidateAll();
        }
    }

    public string Fam2
    {
        get => Medic?.fam2 ?? string.Empty;
        set
        {
            if (Medic == null || Medic.fam2 == value) return;
            Medic.fam2 = value;
            OnPropertyChanged();
            // Cascade: clearing Fam2 invalidates Fam3 (fam3Order rule: fam3 needs fam2).
            // `family` is intentionally NOT cleared — familyOrder depends on fam1 only.
            if (string.IsNullOrWhiteSpace(value))
            {
                Medic.fam3 = string.Empty;
                OnPropertyChanged(nameof(Fam3));
            }
            OnPropertyChanged(nameof(IsFam2Filled));
            OnPropertyChanged(nameof(IsFam3Filled));
            ValidateAll();
        }
    }

    public string Fam3
    {
        get => Medic?.fam3 ?? string.Empty;
        set
        {
            if (Medic == null || Medic.fam3 == value) return;
            Medic.fam3 = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsFam3Filled));
            ValidateAll();
        }
    }

    // Note: named `Family` (not `Fam4`) because the underlying POCO field is `Medic.family`.
    // Semantically it is the 4th-level Famille and depends on `fam1` only (see familyOrder rule).
    public string Family
    {
        get => Medic?.family ?? string.Empty;
        set
        {
            if (Medic == null || Medic.family == value) return;
            Medic.family = value;
            OnPropertyChanged();
            ValidateAll();
        }
    }

    // ── DCI/Dose/Unit proxies — wired to refresh the summary band on each keystroke ──
    public string Dci1
    {
        get => Medic?.dci1 ?? string.Empty;
        set
        {
            if (Medic == null || Medic.dci1 == value) return;
            Medic.dci1 = value;
            OnPropertyChanged();
            if (string.IsNullOrWhiteSpace(value))
            {
                Medic.dci2 = string.Empty; Medic.dose2 = string.Empty; Medic.u2 = string.Empty;
                Medic.dci3 = string.Empty; Medic.dose3 = string.Empty; Medic.u3 = string.Empty;
                Medic.dci4 = string.Empty; Medic.dose4 = string.Empty; Medic.u4 = string.Empty;
                OnPropertyChanged(nameof(Dci2)); OnPropertyChanged(nameof(Dose2)); OnPropertyChanged(nameof(U2));
                OnPropertyChanged(nameof(Dci3)); OnPropertyChanged(nameof(Dose3)); OnPropertyChanged(nameof(U3));
                OnPropertyChanged(nameof(Dci4)); OnPropertyChanged(nameof(Dose4)); OnPropertyChanged(nameof(U4));
            }
            RefreshComputedDciSummary();
            ValidateAll();
        }
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
        set
        {
            if (Medic == null || Medic.dci2 == value) return;
            Medic.dci2 = value;
            OnPropertyChanged();
            if (string.IsNullOrWhiteSpace(value))
            {
                Medic.dci3 = string.Empty; Medic.dose3 = string.Empty; Medic.u3 = string.Empty;
                Medic.dci4 = string.Empty; Medic.dose4 = string.Empty; Medic.u4 = string.Empty;
                OnPropertyChanged(nameof(Dci3)); OnPropertyChanged(nameof(Dose3)); OnPropertyChanged(nameof(U3));
                OnPropertyChanged(nameof(Dci4)); OnPropertyChanged(nameof(Dose4)); OnPropertyChanged(nameof(U4));
            }
            RefreshComputedDciSummary();
            ValidateAll();
        }
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
        set
        {
            if (Medic == null || Medic.dci3 == value) return;
            Medic.dci3 = value;
            OnPropertyChanged();
            if (string.IsNullOrWhiteSpace(value))
            {
                Medic.dci4 = string.Empty; Medic.dose4 = string.Empty; Medic.u4 = string.Empty;
                OnPropertyChanged(nameof(Dci4)); OnPropertyChanged(nameof(Dose4)); OnPropertyChanged(nameof(U4));
            }
            RefreshComputedDciSummary();
            ValidateAll();
        }
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
        set
        {
            if (Medic == null || Medic.dci4 == value) return;
            Medic.dci4 = value;
            OnPropertyChanged();
            RefreshComputedDciSummary();
            ValidateAll();
        }
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
        OnPropertyChanged(nameof(Fam1));
        OnPropertyChanged(nameof(Fam2));
        OnPropertyChanged(nameof(Fam3));
        OnPropertyChanged(nameof(Family));
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

    partial void OnCompareSearchTextChanged(string value)
    {
        // Debounce: fires 300 ms after the last keystroke so the DB is not
        // queried on every character. Clears results when the box is emptied.
        DebounceSearch(async () =>
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                CompareSearchResults = new ObservableCollection<Medic>();
                return;
            }
            var results = await _repository.FindAsync(m => m.itemname.Contains(value));
            CompareSearchResults = new ObservableCollection<Medic>(results.Take(15));
        });
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

    // ─── Monographie (Sprint 5) ───────────────────────────────────────────
    /// <summary>Liste figée des rubriques insérables dans la monographie.</summary>
    public static readonly string[] Rubriques =
    {
        "INDICATIONS THÉRAPEUTIQUES",
        "POSOLOGIE & ADMINISTRATION",
        "CONTRE-INDICATIONS",
        "MISES EN GARDE & PRÉCAUTIONS",
        "INTERACTIONS MÉDICAMENTEUSES",
        "EFFETS INDÉSIRABLES",
        "SURDOSAGE",
        "PHARMACOCINÉTIQUE",
        "PHARMACODYNAMIE",
        "CONDITIONS DE CONSERVATION",
        "SÉCURITÉ PRÉCLINIQUE",
        "PROCRÉATION & ALLAITEMENT"
    };

    [ObservableProperty]
    private string _selectedRubrique = string.Empty;

    /// <summary>
    /// Ouvre la fenêtre d'aperçu HTML de la monographie courante.
    /// Le HTML est généré à partir du Markdown saisi (champ <c>Medic.monographie</c>)
    /// puis affiché dans une WebView2.
    /// </summary>
    [RelayCommand]
    private void OpenMonographiePreview()
    {
        var html = MonographieHtmlRenderer.Render(
            Medic?.monographie ?? string.Empty,
            Medic?.itemname ?? "Médicament");
        var window = App.Services.GetRequiredService<Views.MonographiePreviewWindow>();
        window.LoadHtml(html);
        window.Owner = System.Windows.Application.Current?.Windows
            .OfType<Views.MedicUpsertDialog>()
            .FirstOrDefault();
        window.ShowDialog();
    }

    private readonly IRepository<Medic> _repository;
    private readonly IRepository<Families> _familyRepository;
    private readonly IRepository<Labos> _laboRepository;
    private readonly IRepository<Dci> _dciRepository;
    private readonly IRepository<Formes> _formeRepository;
    private readonly IRepository<Presents> _presentRepository;
    private readonly IRepository<Voies> _voieRepository;
    private readonly IRepository<Specialites> _specialiteRepository;
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

    [ObservableProperty]
    private ObservableCollection<Specialites> _specialites = new();

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
        IRepository<Specialites> specialiteRepository,
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
        _specialiteRepository = specialiteRepository;
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

            var specialites = await _specialiteRepository.GetAllAsync();
            Specialites = new ObservableCollection<Specialites>(specialites);
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

            // Resolve duplicates / fuzzy matches before persisting
            if (!await ResolveReferenceDuplicatesAsync()) return;

            // Defensive: coalesce all string fields to "" before EF persists.
            // Any path that left a field as null (e.g. third-party paste, future
            // bindings) would otherwise hit NOT NULL constraints on the schema.
            Medic.dci1     ??= string.Empty; Medic.dci2 ??= string.Empty;
            Medic.dci3     ??= string.Empty; Medic.dci4 ??= string.Empty;
            Medic.dose1    ??= string.Empty; Medic.dose2 ??= string.Empty;
            Medic.dose3    ??= string.Empty; Medic.dose4 ??= string.Empty;
            Medic.u1       ??= string.Empty; Medic.u2 ??= string.Empty;
            Medic.u3       ??= string.Empty; Medic.u4 ??= string.Empty;
            Medic.fam1     ??= string.Empty; Medic.fam2 ??= string.Empty;
            Medic.fam3     ??= string.Empty; Medic.family ??= string.Empty;
            Medic.labo     ??= string.Empty; Medic.specialite ??= string.Empty;
            Medic.forme    ??= string.Empty; Medic.voie ??= string.Empty;
            Medic.present  ??= string.Empty; Medic.veic ??= string.Empty;
            Medic.tableau  ??= string.Empty;

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

    /// <summary>
    /// For each reference field on the Medic, checks if the user-typed value matches
    /// an existing canonical entry in the reference list. If so, rewrites the field
    /// to use the existing entry's exact spelling (silent canonical normalization).
    /// If no canonical match but a high-confidence fuzzy match (>= 85) exists, prompts
    /// the user to either use the existing entry or create the new one.
    /// Returns true if the save should proceed, false if the user cancelled.
    /// </summary>
    private async Task<bool> ResolveReferenceDuplicatesAsync()
    {
        // Each tuple: (field-name-for-logging, current value, ref-list item names, setter)
        var fields = new (string Label, string Value, IEnumerable<string> Existing, Action<string> Set)[]
        {
            ("DCI Principal", Medic.dci1, Dcis.Select(d => d.itemname),         v => Medic.dci1 = v),
            ("DCI 2",         Medic.dci2, Dcis.Select(d => d.itemname),         v => Medic.dci2 = v),
            ("DCI 3",         Medic.dci3, Dcis.Select(d => d.itemname),         v => Medic.dci3 = v),
            ("DCI 4",         Medic.dci4, Dcis.Select(d => d.itemname),         v => Medic.dci4 = v),
            ("Famille 1",     Medic.fam1, Families.Select(f => f.itemname),     v => Medic.fam1 = v),
            ("Famille 2",     Medic.fam2, Families.Select(f => f.itemname),     v => Medic.fam2 = v),
            ("Famille 3",     Medic.fam3, Families.Select(f => f.itemname),     v => Medic.fam3 = v),
            ("Famille 4",     Medic.family, Families.Select(f => f.itemname),   v => Medic.family = v),
            ("Laboratoire",   Medic.labo, Labos.Select(l => l.itemname),        v => Medic.labo = v),
            ("Forme",         Medic.forme, Formes.Select(f => f.itemname),      v => Medic.forme = v),
            ("Voie",          Medic.voie, Voies.Select(v2 => v2.itemname),      v => Medic.voie = v),
            ("Présentation",  Medic.present, Presents.Select(p => p.itemname),  v => Medic.present = v),
        };

        foreach (var f in fields)
        {
            if (string.IsNullOrWhiteSpace(f.Value)) continue;

            var canon = NameNormalizer.Canonical(f.Value);
            var existingList = f.Existing.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();

            // 1) Exact canonical match → silently rewrite to the existing spelling.
            var canonicalMatch = existingList.FirstOrDefault(n => NameNormalizer.Canonical(n) == canon);
            if (canonicalMatch != null)
            {
                if (canonicalMatch != f.Value) f.Set(canonicalMatch);
                continue;
            }

            // 2) No canonical match — try fuzzy >= 85 against existing entries.
            var best = existingList
                .Select(n => new { Name = n, Score = Fuzz.TokenSortRatio(f.Value, n) })
                .Where(x => x.Score >= 85)
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            if (best == null) continue;  // No fuzzy hit → will be inserted as new entry.

            // 3) Ask the user.
            var useExisting = await _dialogService.ShowConfirmAsync(
                $"{f.Label} — entrée similaire détectée",
                $"« {f.Value} » n'existe pas dans le référentiel.\n\n" +
                $"Une entrée similaire existe : « {best.Name} »\n" +
                $"Score de similarité : {best.Score}/100.\n\n" +
                $"Utiliser « {best.Name} » à la place ?\n" +
                $"(Cliquer « Non » pour créer la nouvelle entrée tel quel.)");

            if (useExisting) f.Set(best.Name);
            // If user said No, leave the value as-is — SyncLookupTablesAsync will create it.
        }

        return true;
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
            // Resolve DCI spelling against the canonical ref list (Dcis is already
            // loaded for the dialog) so we don't grow accent-fragmented duplicates.
            string Canonicalize(string raw)
            {
                var t = raw?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(t)) return string.Empty;
                var match = Dcis.FirstOrDefault(d =>
                    AVCNDB.WPF.Helpers.NameNormalizer.AreSame(d.itemname, t));
                return match?.itemname ?? t;
            }

            var now = DateTime.Now;
            var interact = new Interact
            {
                dci1        = Canonicalize(Medic.dci1),
                dci2        = Canonicalize(SelectedCompareDrug.dci1),
                voie1       = Medic.voie?.Trim() ?? string.Empty,
                voie2       = SelectedCompareDrug.voie?.Trim() ?? string.Empty,
                level       = CapLevel(DeepAnalysisLevel),
                description = DeepAnalysisDescription ?? string.Empty,
                mecanisme   = DeepAnalysisMecanisme   ?? string.Empty,
                conduite    = DeepAnalysisConduite    ?? string.Empty,
                source      = "ai",
                model       = _openRouterService.ModelName,
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

    // level is VARCHAR(30); always one of the 5 canonical values produced by
    // OpenRouterService.NormalizeLevel. description / mecanisme / conduite are
    // TEXT (no length cap) — silent truncation was hiding clinical detail.
    private const int LevelMaxLength = 30;

    private static string CapLevel(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= LevelMaxLength ? value : value.Substring(0, LevelMaxLength);
    }

    // INavigationAware — not used in dialog mode
    public override void OnNavigatedTo(object? parameter) { }
    public override void OnNavigatedFrom() { }
}
