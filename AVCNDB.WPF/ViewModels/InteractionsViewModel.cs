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
    private async Task AnalyzeWithAi()
    {
        var a = SelectedDrugA;
        var b = SelectedDrugB;
        if (a == null || b == null) return;

        var dciA  = a.dci1?.Trim() ?? string.Empty;
        var voieA = a.voie?.Trim() ?? string.Empty;
        var dciB  = b.dci1?.Trim() ?? string.Empty;
        var voieB = b.voie?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(dciA) || string.IsNullOrWhiteSpace(dciB))
        {
            await _dialogService.ShowWarningAsync("DCI manquante",
                "Ce médicament n'a pas de DCI principal renseigné. Modifiez le médicament avant l'analyse.");
            return;
        }

        _analysisCts?.Cancel();
        _analysisCts = new CancellationTokenSource();
        var ct = _analysisCts.Token;

        IsDeepAnalysisRunning = true;
        DeepAnalysisHasPendingResult = false;

        try
        {
            // Local lookup runs first so the user sees something while the AI call is in flight.
            // Voie filter matches MedicUpsertDialog.LoadInteractionsAsync — empty voie is legacy
            // wildcard per Models/Interact.cs.
            var rows = await _interactRepository.FindAsync(i =>
                ((i.dci1 == dciA && (string.IsNullOrEmpty(i.voie1) || i.voie1 == voieA)) &&
                 (i.dci2 == dciB && (string.IsNullOrEmpty(i.voie2) || i.voie2 == voieB)))
              ||
                ((i.dci1 == dciB && (string.IsNullOrEmpty(i.voie1) || i.voie1 == voieB)) &&
                 (i.dci2 == dciA && (string.IsNullOrEmpty(i.voie2) || i.voie2 == voieA))));

            LocalInteractions = new ObservableCollection<Interact>(rows);

            var analysis = await _openRouterService.AnalyzeInteractionAsync(
                dciA, voieA, dciB, voieB, ct);

            DeepAnalysisLevel       = analysis.Level;
            DeepAnalysisDescription = analysis.Description;
            DeepAnalysisMecanisme   = analysis.Mecanisme;
            DeepAnalysisConduite    = analysis.Conduite;
            DeepAnalysisHasPendingResult = true;
        }
        catch (OperationCanceledException)
        {
            // Re-clicked; the newer call is already running. Stay quiet.
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "AnalyzeWithAi failed for {DciA}/{VoieA} vs {DciB}/{VoieB}", dciA, voieA, dciB, voieB);
            await _dialogService.ShowErrorAsync("Analyse IA", BuildAiErrorMessage(ex));
        }
        finally
        {
            IsDeepAnalysisRunning = false;
        }
    }

    private static string BuildAiErrorMessage(Exception ex)
    {
        // Map common HTTP statuses surfaced by OpenRouterService into user-readable French.
        var msg = ex.Message ?? string.Empty;
        if (ex is System.Net.Http.HttpRequestException)
        {
            if (msg.Contains("401")) return "Clé API OpenRouter invalide ou expirée. Vérifiez secrets.json.";
            if (msg.Contains("429")) return "Limite de requêtes OpenRouter atteinte. Réessayez dans quelques instants.";
            if (msg.Contains("500") || msg.Contains("502") || msg.Contains("503") || msg.Contains("504"))
                return "Le service OpenRouter est temporairement indisponible. Réessayez plus tard.";
            return "Connexion à OpenRouter impossible. Vérifiez votre connexion internet et réessayez.";
        }
        // InvalidOperationException from OpenRouterService — its message is already French and actionable.
        return ex.Message ?? string.Empty;
    }

    [RelayCommand]
    private async Task ApproveAndSaveInteraction()
    {
        if (_isSaving) return;
        if (!DeepAnalysisHasPendingResult) return;
        var a = SelectedDrugA;
        var b = SelectedDrugB;
        if (a == null || b == null) return;

        var dciA  = a.dci1?.Trim() ?? string.Empty;
        var voieA = a.voie?.Trim() ?? string.Empty;
        var dciB  = b.dci1?.Trim() ?? string.Empty;
        var voieB = b.voie?.Trim() ?? string.Empty;

        var primary = await _dialogService.ShowConfirmAsync(
            "Enregistrer l'interaction",
            $"Sauvegarder l'interaction IA entre '{dciA}' et '{dciB}' ?\n\nNiveau : {DeepAnalysisLevel}");
        if (!primary) return;

        if (LocalContainsTuple(dciA, voieA, dciB, voieB))
        {
            var keepGoing = await _dialogService.ShowConfirmAsync(
                "Doublon",
                "Une interaction pour cette paire DCI×voie existe déjà. L'ajouter quand même ?");
            if (!keepGoing) return;
        }

        _isSaving = true;
        try
        {
            var now = DateTime.Now;
            var interact = new Interact
            {
                dci1 = dciA, dci2 = dciB,
                voie1 = voieA, voie2 = voieB,
                level       = DeepAnalysisLevel,
                description = DeepAnalysisDescription,
                mecanisme   = DeepAnalysisMecanisme,
                conduite    = DeepAnalysisConduite,
                addedat     = now,
                updatedat   = now
            };
            await _interactRepository.AddAsync(interact);

            // Refresh local rows so the new row appears in the card stack.
            var refreshed = await _interactRepository.FindAsync(i =>
                ((i.dci1 == dciA && (string.IsNullOrEmpty(i.voie1) || i.voie1 == voieA)) &&
                 (i.dci2 == dciB && (string.IsNullOrEmpty(i.voie2) || i.voie2 == voieB)))
              ||
                ((i.dci1 == dciB && (string.IsNullOrEmpty(i.voie1) || i.voie1 == voieB)) &&
                 (i.dci2 == dciA && (string.IsNullOrEmpty(i.voie2) || i.voie2 == voieA))));
            LocalInteractions = new ObservableCollection<Interact>(refreshed);

            DiscardDeepAnalysis();
            await _dialogService.ShowSuccessAsync("Succès",
                "Interaction enregistrée dans la base de données.");
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "ApproveAndSaveInteraction failed for {DciA} vs {DciB}", dciA, dciB);
            await _dialogService.ShowErrorAsync("Erreur", ex.Message);
        }
        finally
        {
            _isSaving = false;
        }
    }

    private bool LocalContainsTuple(string dciA, string voieA, string dciB, string voieB)
    {
        bool MatchesDirection(Interact r, string d1, string v1, string d2, string v2) =>
            string.Equals(r.dci1, d1, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.dci2, d2, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.voie1 ?? string.Empty, v1, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.voie2 ?? string.Empty, v2, StringComparison.OrdinalIgnoreCase);

        return LocalInteractions.Any(r =>
            MatchesDirection(r, dciA, voieA, dciB, voieB) ||
            MatchesDirection(r, dciB, voieB, dciA, voieA));
    }

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
