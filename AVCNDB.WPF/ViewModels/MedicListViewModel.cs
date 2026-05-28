using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Helpers;
using AVCNDB.WPF.Messages;
using AVCNDB.WPF.Models;
using AVCNDB.WPF.Views;

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
    private readonly IRepository<Dci> _dciRepository;
    private readonly IRepository<Formes> _formeRepository;
    private readonly IRepository<Presents> _presentRepository;
    private readonly IRepository<Voies> _voieRepository;
    private readonly IRepository<Catveic> _catveicRepository;
    private readonly IRepository<Specialites> _specialiteRepository;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IExcelService _excelService;
    private readonly IPdfService _pdfService;
    private readonly IStrictExcelSyncService<Medic> _strictExcelSyncService;

    [ObservableProperty]
    private ObservableCollection<Medic> _medics = new();

    [ObservableProperty]
    private Medic? _selectedMedic;

    private bool _isInitializing = true;

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

    [ObservableProperty]
    private bool _showFilters;

    // Collections pour les filtres ComboBox
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

    [ObservableProperty]
    private ObservableCollection<Specialites> _specialites = new();

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
        IRepository<Dci> dciRepository,
        IRepository<Formes> formeRepository,
        IRepository<Presents> presentRepository,
        IRepository<Voies> voieRepository,
        IRepository<Catveic> catveicRepository,
        IRepository<Specialites> specialiteRepository,
        INavigationService navigationService,
        IDialogService dialogService,
        IExcelService excelService,
        IPdfService pdfService,
        IStrictExcelSyncService<Medic> strictExcelSyncService)
    {
        _repository = repository;
        _familyRepository = familyRepository;
        _laboRepository = laboRepository;
        _dciRepository = dciRepository;
        _formeRepository = formeRepository;
        _presentRepository = presentRepository;
        _voieRepository = voieRepository;
        _catveicRepository = catveicRepository;
        _specialiteRepository = specialiteRepository;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _excelService = excelService;
        _pdfService = pdfService;
        _strictExcelSyncService = strictExcelSyncService;

        // Register to listen to database updates for medicines
        WeakReferenceMessenger.Default.Register<DataChangedMessage>(this, (r, m) =>
        {
            if (m.Value.EntityType == "Medic")
            {
                App.Current.Dispatcher.InvokeAsync(async () => await LoadDataAsync());
            }
        });

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        _isInitializing = true;
        await LoadFiltersAsync();
        _isInitializing = false;
        await LoadDataAsync();
    }

    private async Task LoadFiltersAsync()
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

            var specialites = await _specialiteRepository.GetAllAsync();
            Specialites = new ObservableCollection<Specialites>(specialites);

            // Ensure the first load starts with no active lookup filters.
            SelectedFamily = null;
            SelectedLabo = null;
            FilterFamily = string.Empty;
            FilterLabo = string.Empty;
        }, "Chargement des filtres...");
    }

    partial void OnSearchTextChanged(string value)
    {
        CurrentPage = 1;
        DebounceSearch(LoadDataAsync, 400);
    }

    partial void OnSelectedFamilyChanged(Families? value)
    {
        if (_isInitializing)
        {
            FilterFamily = string.Empty;
            return;
        }

        FilterFamily = value?.itemname ?? string.Empty;
        CurrentPage = 1;
        _ = LoadDataAsync();
    }

    partial void OnSelectedLaboChanged(Labos? value)
    {
        if (_isInitializing)
        {
            FilterLabo = string.Empty;
            return;
        }

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

    partial void OnPageSizeChanged(int value)
    {
        if (value <= 0)
        {
            return;
        }

        if (CurrentPage != 1)
        {
            CurrentPage = 1;
            return;
        }

        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            var result = await _repository.GetPagedAsync(
                CurrentPage,
                PageSize,
                m => (string.IsNullOrEmpty(SearchText) ||
                      (m.itemname != null && m.itemname.Contains(SearchText)) ||
                      (m.dci != null && m.dci.Contains(SearchText)) ||
                      (m.barcode != null && m.barcode.Contains(SearchText))) &&
                     (!ShowActiveOnly || m.isactive == 1) &&
                     (string.IsNullOrEmpty(FilterLabo) || (m.labo != null && m.labo.Contains(FilterLabo))) &&
                     (string.IsNullOrEmpty(FilterFamily) ||
                      (m.family != null && m.family.Contains(FilterFamily)) ||
                      (m.fam1 != null && m.fam1.Contains(FilterFamily)) ||
                      (m.fam2 != null && m.fam2.Contains(FilterFamily)) ||
                      (m.fam3 != null && m.fam3.Contains(FilterFamily))),
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
    private async Task ViewDetail(Medic? medic)
    {
        var target = medic ?? SelectedMedic;
        if (target == null) return;

        try
        {
            _navigationService.NavigateTo<MedicDetailViewModel>(target.recordid);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(
                "Erreur",
                $"Impossible d'ouvrir la fiche du médicament.\n\n{ex.Message}");
        }
    }

    [RelayCommand]
    private async Task NewMedic()
    {
        try
        {
            var dialog = App.GetService<MedicUpsertDialog>();
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            await dialog.InitializeAsync(null);
            var result = dialog.ShowDialog();
            if (result == true)
            {
                await LoadDataAsync();
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(
                "Erreur",
                $"Impossible d'ouvrir le formulaire 'Nouveau Médicament'.\n\n{ex.Message}");
        }
    }

    [RelayCommand]
    private async Task EditMedic(Medic? medic)
    {
        var target = medic ?? SelectedMedic;
        if (target == null) return;

        try
        {
            var dialog = App.GetService<MedicUpsertDialog>();
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            await dialog.InitializeAsync(target.recordid);
            var result = dialog.ShowDialog();
            if (result == true)
            {
                await LoadDataAsync();
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(
                "Erreur",
                $"Impossible d'ouvrir le formulaire d'édition du médicament.\n\n{ex.Message}");
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

        if (!confirm) return;

        // Explicit try/catch — ExecuteAsync swallows exceptions into ErrorMessage
        // without surfacing a popup, which made delete failures invisible.
        try
        {
            IsBusy = true;
            IsLoading = true;
            StatusMessage = "Suppression...";

            await _repository.DeleteAsync(target);
            await LoadDataAsync();

            IsBusy = false;
            IsLoading = false;
            StatusMessage = string.Empty;

            await _dialogService.ShowSuccessAsync(
                "Succès",
                $"« {target.itemname} » a été supprimé avec succès.");
        }
        catch (Exception ex)
        {
            IsBusy = false;
            IsLoading = false;
            StatusMessage = "Erreur";

            var innermost = ex;
            while (innermost.InnerException != null) innermost = innermost.InnerException;

            await _dialogService.ShowErrorAsync(
                "Erreur lors de la suppression",
                $"La suppression de « {target.itemname} » a échoué.\n\n{innermost.Message}");
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
                var checkedItems = Medics.Where(m => m.IsChecked).ToList();
                IEnumerable<Medic> dataToExport;
                string exportInfo;

                if (checkedItems.Count > 0)
                {
                    dataToExport = checkedItems;
                    exportInfo = $"{checkedItems.Count} élément(s) sélectionné(s) exporté(s)";
                }
                else
                {
                    dataToExport = await _repository.GetAllAsync();
                    exportInfo = $"Tous les éléments exportés ({dataToExport.Count()})";
                }

                await _excelService.ExportAsync(dataToExport, filePath, "Médicaments");
                await _dialogService.ShowSuccessAsync("Export réussi", $"{exportInfo}\n{filePath}");
            }, "Export en cours...");
        }
    }

    [RelayCommand]
    private async Task DownloadExcelTemplateAsync()
    {
        var filePath = _dialogService.ShowSaveFileDialog(
            "Excel Files|*.xlsx",
            $"Medicaments_Template_{DateTime.Now:yyyyMMdd}",
            "Télécharger le modèle Excel");

        if (!string.IsNullOrEmpty(filePath))
        {
            await ExecuteAsync(async () =>
            {
                await _strictExcelSyncService.CreateTemplateAsync(filePath, "Médicaments");
                await _dialogService.ShowSuccessAsync(
                    "Modèle généré",
                    $"Modèle Excel créé : {filePath}\nRemplissez les colonnes sans modifier les en-têtes.");
            }, "Génération du modèle...");
        }
    }

    [RelayCommand]
    private async Task ExportToPdfAsync()
    {
        // Stratégie cohérente avec l'export Excel (lignes 471-) :
        //   - si N médicaments sont cochés (IsChecked = true) → on génère
        //     un PDF multi-fiches via GenerateMedicListReportAsync ;
        //   - sinon, fallback sur le médicament actuellement sélectionné
        //     (1 fiche unique via GenerateMedicReportAsync).
        var checkedItems = Medics.Where(m => m.IsChecked).ToList();

        Medic? singleTarget = null;
        if (checkedItems.Count == 0)
        {
            if (SelectedMedic == null)
            {
                await _dialogService.ShowWarningAsync(
                    "Attention",
                    "Cochez un ou plusieurs médicaments dans la liste, ou sélectionnez-en un, puis relancez l'export PDF.");
                return;
            }
            singleTarget = SelectedMedic;
        }

        // Nom de fichier suggéré
        string suggestedName;
        if (singleTarget != null)
        {
            var safeName = (singleTarget.itemname ?? singleTarget.basename ?? "medicament")
                .Replace(" ", "_")
                .Replace("/", "-");
            suggestedName = $"Fiche_{safeName}_{DateTime.Now:yyyyMMdd}";
        }
        else
        {
            suggestedName = $"Fiches_{checkedItems.Count}_medicaments_{DateTime.Now:yyyyMMdd}";
        }

        var filePath = _dialogService.ShowSaveFileDialog(
            "PDF Files|*.pdf",
            suggestedName,
            "Exporter vers PDF");

        if (!string.IsNullOrEmpty(filePath))
        {
            await ExecuteAsync(async () =>
            {
                if (singleTarget != null)
                {
                    await _pdfService.GenerateMedicReportAsync(singleTarget.recordid, filePath);
                    await _dialogService.ShowSuccessAsync(
                        "Export réussi",
                        $"Fiche exportée vers {filePath}");
                }
                else
                {
                    var ids = checkedItems.Select(m => m.recordid).ToList();
                    await _pdfService.GenerateMedicListReportAsync(ids, filePath);
                    await _dialogService.ShowSuccessAsync(
                        "Export réussi",
                        $"{ids.Count} fiches exportées vers {filePath}");
                }
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
                var result = await _strictExcelSyncService.ImportAndSyncAsync(filePath, "Médicaments");

                if (!result.IsValid)
                {
                    await _dialogService.ShowErrorAsync(
                        "Erreur de validation",
                        string.Join("\n", result.Errors));
                    return;
                }

                await LoadDataAsync();
                await _dialogService.ShowSuccessAsync(
                    "Import Excel terminé",
                    $"Lignes lues : {result.RowCount}\nInsérés : {result.InsertedCount}\nMis à jour : {result.UpdatedCount}\nIgnorés : {result.SkippedCount}");
            }, "Import en cours...");
        }
    }

    public override void Dispose()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        base.Dispose();
    }
}
