using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.ViewModels;

/// <summary>
/// ViewModel for the Interactions page.
///
/// User picks drug A and drug B (each search box renders results as
/// drug-name on top, "DCI: {dci1} · Voie: {voie}" sublabel underneath).
/// On *Analyser avec IA*: the local `interact` table is queried for the
/// resolved (DCI × Voie) pair, then OpenRouter is called for a fresh
/// analysis that the user can approve into the local table.
/// </summary>
public partial class InteractionsViewModel : ViewModelBase
{
    private readonly IRepository<Interact> _interactRepository;
    private readonly IRepository<Medic> _medicRepository;
    private readonly IOpenRouterService _openRouterService;
    private readonly IDialogService _dialogService;
    private readonly IPdfService _pdfService;
    private readonly IMLPfeService _mlPfeService;

    /// <summary>
    /// CancellationTokenSource for the in-flight AI analysis. Cancelled and
    /// replaced at the start of every AnalyzeWithAiAsync, and from the
    /// view's Unloaded handler via <see cref="CancelInFlightAnalysis"/>.
    /// </summary>
    private CancellationTokenSource? _analysisCts;

    /// <summary>Guards <see cref="ApproveAndSaveInteractionCommand"/> against double-click.</summary>
    private bool _isSaving;

    // ── Drug A slot ──
    [ObservableProperty] private string _drugASearchText = string.Empty;
    [ObservableProperty] private ObservableCollection<Medic> _drugASearchResults = new();
    [ObservableProperty] private Medic? _selectedDrugA;
    [ObservableProperty] private bool _isDrugASearchRunning;

    // ── Drug B slot ──
    [ObservableProperty] private string _drugBSearchText = string.Empty;
    [ObservableProperty] private ObservableCollection<Medic> _drugBSearchResults = new();
    [ObservableProperty] private Medic? _selectedDrugB;
    [ObservableProperty] private bool _isDrugBSearchRunning;

    // ── Local lookup ──
    [ObservableProperty] private ObservableCollection<Interact> _localInteractions = new();

    // ── AI pending result ──
    [ObservableProperty] private bool _isDeepAnalysisRunning;
    [ObservableProperty] private bool _deepAnalysisHasPendingResult;
    [ObservableProperty] private string _deepAnalysisLevel = string.Empty;
    [ObservableProperty] private string _deepAnalysisDescription = string.Empty;
    [ObservableProperty] private string _deepAnalysisMecanisme = string.Empty;
    [ObservableProperty] private string _deepAnalysisConduite = string.Empty;

    // ── Derived flags ──
    public bool CanAnalyze =>
        SelectedDrugA != null && SelectedDrugB != null && !IsDeepAnalysisRunning;

    public bool HasResults =>
        LocalInteractions.Count > 0 || DeepAnalysisHasPendingResult;

    public bool NoResults => !HasResults;

    public InteractionsViewModel(
        IRepository<Interact> interactRepository,
        IRepository<Medic> medicRepository,
        IOpenRouterService openRouterService,
        IDialogService dialogService,
        IPdfService pdfService,
        IMLPfeService mlPfeService)
    {
        _interactRepository = interactRepository;
        _medicRepository = medicRepository;
        _openRouterService = openRouterService;
        _dialogService = dialogService;
        _pdfService = pdfService;
        _mlPfeService = mlPfeService;
    }

    // ── Search-text debounce hooks ──

    partial void OnDrugASearchTextChanged(string value)
    {
        DebounceSearch(() => RunDrugASearch());
    }

    partial void OnDrugBSearchTextChanged(string value)
    {
        DebounceSearch(() => RunDrugBSearch());
    }

    // ── Selection side effects ──

    partial void OnSelectedDrugAChanged(Medic? value)
    {
        DiscardDeepAnalysis();
        LocalInteractions = new ObservableCollection<Interact>();
        OnPropertyChanged(nameof(CanAnalyze));
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(NoResults));
    }

    partial void OnSelectedDrugBChanged(Medic? value)
    {
        if (value != null && SelectedDrugA != null && SelectedDrugA.recordid == value.recordid)
        {
            // Re-entrancy: setting SelectedDrugB to null below will re-fire this method.
            // The next call sees value == null, skips this branch, and proceeds to clear state.
            SelectedDrugB = null;
            _ = _dialogService.ShowWarningAsync("Médicament identique",
                "Choisissez deux médicaments différents.");
            return;
        }
        DiscardDeepAnalysis();
        LocalInteractions = new ObservableCollection<Interact>();
        OnPropertyChanged(nameof(CanAnalyze));
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(NoResults));
    }

    partial void OnIsDeepAnalysisRunningChanged(bool value)
    {
        OnPropertyChanged(nameof(CanAnalyze));
    }

    partial void OnDeepAnalysisHasPendingResultChanged(bool value)
    {
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(NoResults));
    }

    partial void OnLocalInteractionsChanged(ObservableCollection<Interact> value)
    {
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(NoResults));
    }

    // ── Commands ──

    [RelayCommand]
    private async Task RunDrugASearch()
    {
        var text = DrugASearchText?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(text))
        {
            DrugASearchResults = new ObservableCollection<Medic>();
            return;
        }

        IsDrugASearchRunning = true;
        try
        {
            var hits = await _medicRepository.FindAsync(
                m => m.itemname.Contains(text) && m.isactive == 1);

            // Stale-write guard: if the user kept typing while we were awaiting,
            // the current DrugASearchText no longer matches the query we issued.
            // Discard the result so we don't overwrite a newer search's output.
            if ((DrugASearchText?.Trim() ?? string.Empty) != text) return;

            DrugASearchResults = new ObservableCollection<Medic>(
                hits.OrderBy(m => m.itemname).Take(50));
        }
        finally
        {
            IsDrugASearchRunning = false;
        }
    }

    [RelayCommand]
    private async Task RunDrugBSearch()
    {
        var text = DrugBSearchText?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(text))
        {
            DrugBSearchResults = new ObservableCollection<Medic>();
            return;
        }

        IsDrugBSearchRunning = true;
        try
        {
            var hits = await _medicRepository.FindAsync(
                m => m.itemname.Contains(text) && m.isactive == 1);

            // Stale-write guard: if the user kept typing while we were awaiting,
            // the current DrugBSearchText no longer matches the query we issued.
            // Discard the result so we don't overwrite a newer search's output.
            if ((DrugBSearchText?.Trim() ?? string.Empty) != text) return;

            DrugBSearchResults = new ObservableCollection<Medic>(
                hits.OrderBy(m => m.itemname).Take(50));
        }
        finally
        {
            IsDrugBSearchRunning = false;
        }
    }

    [RelayCommand]
    private void ClearDrugA() { SelectedDrugA = null; }

    [RelayCommand]
    private void ClearDrugB() { SelectedDrugB = null; }

    [RelayCommand]
    private Task AnalyzeWithAi() => Task.CompletedTask;

    [RelayCommand]
    private Task ApproveAndSaveInteraction() => Task.CompletedTask;

    [RelayCommand]
    private void DiscardDeepAnalysis()
    {
        DeepAnalysisLevel = string.Empty;
        DeepAnalysisDescription = string.Empty;
        DeepAnalysisMecanisme = string.Empty;
        DeepAnalysisConduite = string.Empty;
        DeepAnalysisHasPendingResult = false;
    }

    [RelayCommand]
    private Task ExportPdf() => Task.CompletedTask;

    [RelayCommand]
    private Task LaunchMlPfe() => Task.CompletedTask;

    /// <summary>Public entry point for the view's Unloaded handler.</summary>
    public void CancelInFlightAnalysis() => _analysisCts?.Cancel();
}
