using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Models;
using AVCNDB.WPF.Services;

namespace AVCNDB.WPF.ViewModels;

/// <summary>
/// ViewModel pour l'édition/création d'un médicament
/// </summary>
public partial class MedicEditViewModel : ViewModelBase
{
    private readonly IRepository<Medic> _repository;
    private readonly IRepository<Families> _familyRepository;
    private readonly IRepository<Labos> _laboRepository;
    private readonly IRepository<Dci> _dciRepository;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly MedicSyncService _syncService;

    private int? _medicId;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private bool _isNewMedic;

    [ObservableProperty]
    private string _pageTitle = "Nouveau Médicament";

    [ObservableProperty]
    private Medic _medic = new();

    // Collections pour les ComboBox
    [ObservableProperty]
    private ObservableCollection<Families> _families = new();

    [ObservableProperty]
    private ObservableCollection<Labos> _labos = new();

    [ObservableProperty]
    private ObservableCollection<Dci> _dcis = new();

    // Erreurs de validation
    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    public MedicEditViewModel(
        IRepository<Medic> repository,
        IRepository<Families> familyRepository,
        IRepository<Labos> laboRepository,
        IRepository<Dci> dciRepository,
        INavigationService navigationService,
        IDialogService dialogService,
        MedicSyncService syncService)
    {
        _repository = repository;
        _familyRepository = familyRepository;
        _laboRepository = laboRepository;
        _dciRepository = dciRepository;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _syncService = syncService;
    }

    public override void OnNavigatedTo(object? parameter)
    {
        _ = InitializeAsync(parameter);
    }

    private async Task InitializeAsync(object? parameter)
    {
        await LoadReferenceDataAsync();

        if (parameter is int medicId)
        {
            _medicId = medicId;
            IsEditMode = true;
            IsNewMedic = false;
            PageTitle = "Modifier le Médicament";
            await LoadMedicAsync(medicId);
        }
        else
        {
            _medicId = null;
            IsEditMode = false;
            IsNewMedic = true;
            PageTitle = "Nouveau Médicament";
            Medic = new Medic { isactive = 1 };
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
        }, "Chargement des données de référence...");
    }

    public override void OnNavigatedFrom() { }

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

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!Validate())
        {
            await _dialogService.ShowWarningAsync("Validation", 
                ErrorMessage ?? "Veuillez corriger les erreurs avant de sauvegarder.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            if (IsEditMode && _medicId.HasValue)
            {
                await _repository.UpdateAsync(Medic);
            }
            else
            {
                await _repository.AddAsync(Medic);
            }

            // Synchroniser les tables de référence (DCI, Familles, Labos, Formes, Voies)
            try { await _syncService.SyncLookupTablesAsync(Medic); } catch { /* non-fatal */ }

            await _dialogService.ShowSuccessAsync("Succès", 
                IsEditMode ? "Médicament mis à jour avec succès." : "Médicament créé avec succès.");
            
            _navigationService.GoBack();
        }, "Sauvegarde en cours...");
    }

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

        _navigationService.GoBack();
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}
