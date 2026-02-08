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

    // ── Résultats ──
    [ObservableProperty]
    private ObservableCollection<Interact> _interactions = new();

    [ObservableProperty]
    private bool _hasResults;

    [ObservableProperty]
    private bool _noResults = true;

    // ── Panneau gauche : recherche et sélection DCI ──
    [ObservableProperty]
    private string _dciSearchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<DciSelectItem> _availableDcis = new();

    [ObservableProperty]
    private ObservableCollection<DciSelectItem> _selectedDcis = new();

    [ObservableProperty]
    private bool _canAnalyze;

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

        _ = LoadDcisAsync();
    }

    private async Task LoadDcisAsync()
    {
        await ExecuteAsync(async () =>
        {
            var dcis = await _dciRepository.GetAllAsync();
            AvailableDcis = new ObservableCollection<DciSelectItem>(
                dcis.OrderBy(d => d.itemname)
                    .Select(d => new DciSelectItem { Dciname = d.itemname }));
        }, "Chargement des DCI...");
    }

    partial void OnDciSearchTextChanged(string value)
    {
        _ = FilterDcisAsync(value);
    }

    private async Task FilterDcisAsync(string search)
    {
        var dcis = string.IsNullOrWhiteSpace(search)
            ? await _dciRepository.GetAllAsync()
            : await _dciRepository.FindAsync(d => d.itemname.Contains(search));

        AvailableDcis = new ObservableCollection<DciSelectItem>(
            dcis.OrderBy(d => d.itemname)
                .Select(d => new DciSelectItem
                {
                    Dciname = d.itemname,
                    IsSelected = SelectedDcis.Any(s => s.Dciname == d.itemname)
                }));
    }

    [RelayCommand]
    private void SearchDci() { /* triggered by Enter key, OnDciSearchTextChanged handles it */ }

    [RelayCommand]
    private void AddDci(DciSelectItem? dci)
    {
        if (dci == null || string.IsNullOrWhiteSpace(dci.Dciname)) return;
        if (SelectedDcis.Any(s => s.Dciname == dci.Dciname)) return;

        SelectedDcis.Add(new DciSelectItem { Dciname = dci.Dciname, IsSelected = true });
        dci.IsSelected = true;
        CanAnalyze = SelectedDcis.Count >= 2;
    }

    [RelayCommand]
    private void RemoveDci(DciSelectItem? dci)
    {
        if (dci == null) return;

        var item = SelectedDcis.FirstOrDefault(s => s.Dciname == dci.Dciname);
        if (item != null) SelectedDcis.Remove(item);

        // Also uncheck in available list
        var available = AvailableDcis.FirstOrDefault(a => a.Dciname == dci.Dciname);
        if (available != null) available.IsSelected = false;

        CanAnalyze = SelectedDcis.Count >= 2;
    }

    [RelayCommand]
    private async Task Analyze()
    {
        if (SelectedDcis.Count < 2)
        {
            await _dialogService.ShowWarningAsync("Attention",
                "Veuillez sélectionner au moins 2 DCI pour analyser les interactions.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            var dciNames = SelectedDcis.Select(d => d.Dciname).ToList();
            var found = new List<Interact>();

            for (int i = 0; i < dciNames.Count; i++)
            {
                for (int j = i + 1; j < dciNames.Count; j++)
                {
                    var d1 = dciNames[i];
                    var d2 = dciNames[j];

                    var interactions = await _repository.FindAsync(inter =>
                        (inter.dci1.Contains(d1) && inter.dci2.Contains(d2)) ||
                        (inter.dci1.Contains(d2) && inter.dci2.Contains(d1)));

                    found.AddRange(interactions);
                }
            }

            Interactions = new ObservableCollection<Interact>(found);
            HasResults = found.Count > 0;
            NoResults = found.Count == 0;
        }, "Analyse en cours...");
    }

    [RelayCommand]
    private async Task ExportPdf()
    {
        if (!HasResults && SelectedDcis.Count == 0)
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
            try
            {
                var dciNames = SelectedDcis.Select(d => d.Dciname);
                await _pdfService.GenerateInteractionReportAsync(dciNames, filePath);

                if (System.IO.File.Exists(filePath))
                {
                    await _dialogService.ShowSuccessAsync("Export réussi",
                        $"Rapport d'interactions exporté vers :\n{filePath}");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("Erreur d'export",
                    $"Impossible de générer le PDF :\n{ex.Message}");
            }
        }
    }
}

/// <summary>
/// Item de sélection DCI pour l'analyse d'interactions
/// </summary>
public partial class DciSelectItem : ObservableObject
{
    [ObservableProperty]
    private string _dciname = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}
