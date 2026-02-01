using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.ViewModels;

/// <summary>
/// ViewModel pour la gestion des interactions médicamenteuses
/// </summary>
public partial class InteractionsViewModel : ViewModelBase
{
    private readonly IRepository<Interact> _repository;
    private readonly IRepository<Dci> _dciRepository;
    private readonly IDialogService _dialogService;
    private readonly IPdfService _pdfService;

    [ObservableProperty]
    private ObservableCollection<Interact> _interactions = new();

    [ObservableProperty]
    private Interact? _selectedInteraction;

    [ObservableProperty]
    private string _searchDci1 = string.Empty;

    [ObservableProperty]
    private string _searchDci2 = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _dciSuggestions = new();

    // Pour l'analyse de prescription
    [ObservableProperty]
    private ObservableCollection<string> _selectedDcis = new();

    [ObservableProperty]
    private ObservableCollection<Interact> _foundInteractions = new();

    [ObservableProperty]
    private bool _isAnalyzing;

    [ObservableProperty]
    private string _analysisResult = string.Empty;

    public InteractionsViewModel(
        IRepository<Interact> repository,
        IRepository<Dci> dciRepository,
        IDialogService dialogService,
        IPdfService pdfService)
    {
        _repository = repository;
        _dciRepository = dciRepository;
        _dialogService = dialogService;
        _pdfService = pdfService;

        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            var items = await _repository.GetAllAsync();
            Interactions = new ObservableCollection<Interact>(
                items.OrderBy(i => i.dci1).ThenBy(i => i.dci2));
        }, "Chargement des interactions...");
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await ExecuteAsync(async () =>
        {
            IEnumerable<Interact> items;

            if (!string.IsNullOrEmpty(SearchDci1) && !string.IsNullOrEmpty(SearchDci2))
            {
                items = await _repository.FindAsync(i =>
                    (i.dci1.Contains(SearchDci1) && i.dci2.Contains(SearchDci2)) ||
                    (i.dci1.Contains(SearchDci2) && i.dci2.Contains(SearchDci1)));
            }
            else if (!string.IsNullOrEmpty(SearchDci1))
            {
                items = await _repository.FindAsync(i =>
                    i.dci1.Contains(SearchDci1) || i.dci2.Contains(SearchDci1));
            }
            else
            {
                items = await _repository.GetAllAsync();
            }

            Interactions = new ObservableCollection<Interact>(items);
        }, "Recherche...");
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchDci1 = string.Empty;
        SearchDci2 = string.Empty;
        _ = LoadDataAsync();
    }

    [RelayCommand]
    private void AddDciToAnalysis(string dci)
    {
        if (!string.IsNullOrWhiteSpace(dci) && !SelectedDcis.Contains(dci))
        {
            SelectedDcis.Add(dci);
        }
    }

    [RelayCommand]
    private void RemoveDciFromAnalysis(string dci)
    {
        SelectedDcis.Remove(dci);
    }

    [RelayCommand]
    private async Task AnalyzeInteractionsAsync()
    {
        if (SelectedDcis.Count < 2)
        {
            await _dialogService.ShowWarningAsync("Attention", 
                "Veuillez sélectionner au moins 2 DCI pour analyser les interactions.");
            return;
        }

        IsAnalyzing = true;

        await ExecuteAsync(async () =>
        {
            var dcis = SelectedDcis.ToList();
            var found = new List<Interact>();

            // Chercher toutes les interactions entre les DCI sélectionnées
            for (int i = 0; i < dcis.Count; i++)
            {
                for (int j = i + 1; j < dcis.Count; j++)
                {
                    var dci1 = dcis[i];
                    var dci2 = dcis[j];

                    var interactions = await _repository.FindAsync(inter =>
                        (inter.dci1.Contains(dci1) && inter.dci2.Contains(dci2)) ||
                        (inter.dci1.Contains(dci2) && inter.dci2.Contains(dci1)));

                    found.AddRange(interactions);
                }
            }

            FoundInteractions = new ObservableCollection<Interact>(found);

            if (found.Any())
            {
                var contraindicated = found.Count(i => 
                    i.level?.ToLower().Contains("contre-indication") == true);
                var discouraged = found.Count(i => 
                    i.level?.ToLower().Contains("déconseillée") == true);
                var precaution = found.Count(i => 
                    i.level?.ToLower().Contains("précaution") == true);

                AnalysisResult = $"⚠ {found.Count} interaction(s) trouvée(s):\n" +
                                 $"• Contre-indications: {contraindicated}\n" +
                                 $"• Associations déconseillées: {discouraged}\n" +
                                 $"• Précautions d'emploi: {precaution}";
            }
            else
            {
                AnalysisResult = "✅ Aucune interaction connue entre les DCI sélectionnées.";
            }
        }, "Analyse en cours...");

        IsAnalyzing = false;
    }

    [RelayCommand]
    private void ClearAnalysis()
    {
        SelectedDcis.Clear();
        FoundInteractions.Clear();
        AnalysisResult = string.Empty;
    }

    [RelayCommand]
    private async Task ExportAnalysisAsync()
    {
        if (!SelectedDcis.Any())
        {
            await _dialogService.ShowWarningAsync("Attention", 
                "Veuillez d'abord sélectionner des DCI et lancer une analyse.");
            return;
        }

        var filePath = _dialogService.ShowSaveFileDialog(
            "PDF Files|*.pdf",
            $"Analyse_Interactions_{DateTime.Now:yyyyMMdd_HHmm}",
            "Exporter l'analyse");

        if (!string.IsNullOrEmpty(filePath))
        {
            await ExecuteAsync(async () =>
            {
                await _pdfService.GenerateInteractionReportAsync(SelectedDcis, filePath);
                await _dialogService.ShowSuccessAsync("Export réussi", 
                    $"Rapport d'interactions exporté vers {filePath}");
            }, "Génération du rapport...");
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDataAsync();
    }
}
