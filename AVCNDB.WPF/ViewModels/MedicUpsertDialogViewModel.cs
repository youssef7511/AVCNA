using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Messages;
using AVCNDB.WPF.Models;
using AVCNDB.WPF.Services;

namespace AVCNDB.WPF.ViewModels;

public partial class MedicUpsertDialogViewModel : ViewModelBase
{
    private readonly IRepository<Medic> _repository;
    private readonly IRepository<Families> _familyRepository;
    private readonly IRepository<Labos> _laboRepository;
    private readonly IRepository<Dci> _dciRepository;
    private readonly IRepository<Formes> _formeRepository;
    private readonly IRepository<Presents> _presentRepository;
    private readonly IRepository<Voies> _voieRepository;
    private readonly IRepository<Catveic> _catveicRepository;
    private readonly IDialogService _dialogService;
    private readonly MedicSyncService _syncService;

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
    private ObservableCollection<Catveic> _catveics = new();

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
        IRepository<Catveic> catveicRepository,
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
        _catveicRepository = catveicRepository;
        _dialogService = dialogService;
        _syncService = syncService;
    }

    /// <summary>
    /// Initialise the dialog for a new or existing medic.
    /// Called by the dialog code-behind after construction.
    /// </summary>
    public async Task InitializeAsync(int? medicId)
    {
        await LoadReferenceDataAsync();

        if (medicId.HasValue)
        {
            IsEditMode = true;
            PageTitle = "Modifier le Médicament";
            await LoadMedicAsync(medicId.Value);
        }
        else
        {
            IsEditMode = false;
            PageTitle = "Nouveau Médicament";
            Medic = new Medic { isactive = 1 };
        }

        RefreshComputedDenomination();
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

            var catveics = await _catveicRepository.GetAllAsync();
            Catveics = new ObservableCollection<Catveic>(catveics);
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

    // ── Denomination builder ──

    [RelayCommand]
    private void UpdateDenomination()
    {
        Medic.basename = BuildDenomination(Medic);
        RefreshComputedDenomination();
    }

    public void RefreshComputedDenomination()
    {
        ComputedDenomination = BuildDenomination(Medic);
    }

    private static string BuildDenomination(Medic m)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(m.dose1)) parts.Add(m.dose1.Trim());
        if (!string.IsNullOrWhiteSpace(m.u1)) parts.Add(m.u1.Trim());
        if (!string.IsNullOrWhiteSpace(m.dose2)) parts.Add(m.dose2.Trim());
        if (!string.IsNullOrWhiteSpace(m.u2)) parts.Add(m.u2.Trim());
        if (!string.IsNullOrWhiteSpace(m.dose3)) parts.Add(m.dose3.Trim());
        if (!string.IsNullOrWhiteSpace(m.u3)) parts.Add(m.u3.Trim());
        if (!string.IsNullOrWhiteSpace(m.dose4)) parts.Add(m.dose4.Trim());
        if (!string.IsNullOrWhiteSpace(m.u4)) parts.Add(m.u4.Trim());
        if (!string.IsNullOrWhiteSpace(m.forme)) parts.Add(m.forme.Trim());
        if (!string.IsNullOrWhiteSpace(m.present)) parts.Add(m.present.Trim());
        if (m.colisage > 0) parts.Add(m.colisage.ToString());
        if (!string.IsNullOrWhiteSpace(m.ucol)) parts.Add(m.ucol.Trim());

        return string.Join(" ", parts);
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
        HasError = false;
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Medic.itemname))
        {
            ErrorMessage = "Le nom du médicament est obligatoire";
            HasError = true;
            return false;
        }

        return true;
    }

    // ── Save ──

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!Validate())
        {
            await _dialogService.ShowWarningAsync("Validation",
                ErrorMessage ?? "Veuillez corriger les erreurs avant de sauvegarder.");
            return;
        }

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
        var hasChanges = !string.IsNullOrEmpty(Medic.itemname);

        if (hasChanges)
        {
            var confirm = await _dialogService.ShowConfirmAsync(
                "Annuler les modifications",
                "Voulez-vous vraiment annuler ? Les modifications non sauvegardées seront perdues.");

            if (!confirm) return;
        }

        RequestClose?.Invoke(false);
    }

    /// <summary>
    /// The dialog code-behind subscribes to this to close the Window.
    /// Parameter: true = saved, false = cancelled.
    /// </summary>
    public event Action<bool>? RequestClose;

    // INavigationAware — not used in dialog mode
    public override void OnNavigatedTo(object? parameter) { }
    public override void OnNavigatedFrom() { }
}
