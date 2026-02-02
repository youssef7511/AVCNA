using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.ViewModels;

/// <summary>
/// ViewModel pour la liste des médicaments
/// Avec recherche, filtrage, pagination et export
/// </summary>
public partial class MedicListViewModel : ViewModelBase
{
    private readonly IRepository<Medic> _repository;
    private readonly IRepository<Families> _familyRepository;
    private readonly IRepository<Labos> _laboRepository;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IExcelService _excelService;
    private readonly IPdfService _pdfService;

    [ObservableProperty]
    private ObservableCollection<Medic> _medics = new();

    [ObservableProperty]
    private Medic? _selectedMedic;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _pageSize = 50;

    [ObservableProperty]
    private int _totalPages;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private string _filterLabo = string.Empty;

    [ObservableProperty]
    private string _filterFamily = string.Empty;

    [ObservableProperty]
    private bool _showActiveOnly = true;

    // Collections pour les filtres ComboBox
    [ObservableProperty]
    private ObservableCollection<Families> _families = new();

    [ObservableProperty]
    private ObservableCollection<Labos> _labos = new();

    [ObservableProperty]
    private Families? _selectedFamily;

    [ObservableProperty]
    private Labos? _selectedLabo;

    // Propriétés de pagination calculées
    public int StartIndex => TotalCount == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;
    public int EndIndex => Math.Min(CurrentPage * PageSize, TotalCount);
    public bool CanGoPrevious => CurrentPage > 1;
    public bool CanGoNext => CurrentPage < TotalPages;

    public MedicListViewModel(
        IRepository<Medic> repository,
        IRepository<Families> familyRepository,
        IRepository<Labos> laboRepository,
        INavigationService navigationService,
        IDialogService dialogService,
        IExcelService excelService,
        IPdfService pdfService)
    {
        _repository = repository;
        _familyRepository = familyRepository;
        _laboRepository = laboRepository;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _excelService = excelService;
        _pdfService = pdfService;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LoadFiltersAsync();
        await LoadDataAsync();
    }

    private async Task LoadFiltersAsync()
    {
        var families = await _familyRepository.GetAllAsync();
        Families = new ObservableCollection<Families>(families);

        var labos = await _laboRepository.GetAllAsync();
        Labos = new ObservableCollection<Labos>(labos);
    }

    partial void OnSearchTextChanged(string value)
    {
        CurrentPage = 1;
        _ = LoadDataAsync();
    }

    partial void OnSelectedFamilyChanged(Families? value)
    {
        FilterFamily = value?.itemname ?? string.Empty;
        CurrentPage = 1;
        _ = LoadDataAsync();
    }

    partial void OnSelectedLaboChanged(Labos? value)
    {
        FilterLabo = value?.itemname ?? string.Empty;
        CurrentPage = 1;
        _ = LoadDataAsync();
    }

    partial void OnCurrentPageChanged(int value)
    {
        OnPropertyChanged(nameof(StartIndex));
        OnPropertyChanged(nameof(EndIndex));
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
        _ = LoadDataAsync();
    }

    partial void OnTotalPagesChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
    }

    partial void OnTotalCountChanged(int value)
    {
        OnPropertyChanged(nameof(StartIndex));
        OnPropertyChanged(nameof(EndIndex));
    }

    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            var result = await _repository.GetPagedAsync(
                CurrentPage,
                PageSize,
                m => (string.IsNullOrEmpty(SearchText) || 
                      m.itemname.Contains(SearchText) || 
                      m.dci.Contains(SearchText) ||
                      m.barcode.Contains(SearchText)) &&
                     (!ShowActiveOnly || m.isactive == 1) &&
                     (string.IsNullOrEmpty(FilterLabo) || m.labo.Contains(FilterLabo)) &&
                     (string.IsNullOrEmpty(FilterFamily) || m.family.Contains(FilterFamily)),
                m => m.itemname
            );

            Medics = new ObservableCollection<Medic>(result.Items);
            TotalCount = result.TotalCount;
            TotalPages = result.TotalPages;
        }, "Chargement des médicaments...");
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadDataAsync();
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
        FilterLabo = string.Empty;
        FilterFamily = string.Empty;
        SelectedFamily = null;
        SelectedLabo = null;
    }

    [RelayCommand]
    private void FirstPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage = 1;
        }
    }

    [RelayCommand]
    private void LastPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage = TotalPages;
        }
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
        }
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
        }
    }

    [RelayCommand]
    private void ViewDetail(Medic? medic)
    {
        var target = medic ?? SelectedMedic;
        if (target != null)
        {
            _navigationService.NavigateTo<MedicDetailViewModel>(target.recordid);
        }
    }

    [RelayCommand]
    private void NewMedic()
    {
        _navigationService.NavigateTo<MedicEditViewModel>(null);
    }

    [RelayCommand]
    private void EditMedic(Medic? medic)
    {
        var target = medic ?? SelectedMedic;
        if (target != null)
        {
            _navigationService.NavigateTo<MedicEditViewModel>(target.recordid);
        }
    }

    [RelayCommand]
    private async Task DeleteMedic(Medic? medic)
    {
        var target = medic ?? SelectedMedic;
        if (target == null) return;

        var confirm = await _dialogService.ShowConfirmAsync(
            "Confirmer la suppression",
            $"Voulez-vous vraiment supprimer le médicament '{target.itemname}' ?");

        if (confirm)
        {
            await ExecuteAsync(async () =>
            {
                await _repository.DeleteAsync(target);
                await LoadDataAsync();
                await _dialogService.ShowSuccessAsync("Succès", "Médicament supprimé avec succès.");
            });
        }
    }

    [RelayCommand]
    private async Task ExportToExcelAsync()
    {
        var filePath = _dialogService.ShowSaveFileDialog(
            "Excel Files|*.xlsx",
            $"Medicaments_{DateTime.Now:yyyyMMdd}",
            "Exporter vers Excel");

        if (!string.IsNullOrEmpty(filePath))
        {
            await ExecuteAsync(async () =>
            {
                var allMedics = await _repository.GetAllAsync();
                await _excelService.ExportAsync(allMedics, filePath, "Médicaments");
                await _dialogService.ShowSuccessAsync("Export réussi", $"Données exportées vers {filePath}");
            }, "Export en cours...");
        }
    }

    [RelayCommand]
    private async Task ExportToPdfAsync()
    {
        if (SelectedMedic == null)
        {
            await _dialogService.ShowWarningAsync("Attention", "Veuillez sélectionner un médicament.");
            return;
        }

        var filePath = _dialogService.ShowSaveFileDialog(
            "PDF Files|*.pdf",
            $"Fiche_{SelectedMedic.itemname.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}",
            "Exporter vers PDF");

        if (!string.IsNullOrEmpty(filePath))
        {
            await ExecuteAsync(async () =>
            {
                await _pdfService.GenerateMedicReportAsync(SelectedMedic.recordid, filePath);
                await _dialogService.ShowSuccessAsync("Export réussi", $"Fiche exportée vers {filePath}");
            }, "Génération du PDF...");
        }
    }

    [RelayCommand]
    private async Task ImportFromExcelAsync()
    {
        var filePath = _dialogService.ShowOpenFileDialog(
            "Excel Files|*.xlsx;*.xls",
            "Importer depuis Excel");

        if (!string.IsNullOrEmpty(filePath))
        {
            await ExecuteAsync(async () =>
            {
                var validation = await _excelService.ValidateFileAsync(filePath, new[] 
                { 
                    "itemname", "dci", "forme", "voie", "present", "labo" 
                });

                if (!validation.IsValid)
                {
                    await _dialogService.ShowErrorAsync("Erreur de validation", 
                        string.Join("\n", validation.Errors));
                    return;
                }

                var medics = await _excelService.ImportAsync<Medic>(filePath);
                await _repository.AddRangeAsync(medics);
                await LoadDataAsync();
                await _dialogService.ShowSuccessAsync("Import réussi", 
                    $"{validation.RowCount} médicaments importés avec succès.");
            }, "Import en cours...");
        }
    }
}
