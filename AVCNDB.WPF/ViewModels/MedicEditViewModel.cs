using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Messages;
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
    private readonly IRepository<Formes> _formeRepository;
    private readonly IRepository<Presents> _presentRepository;
    private readonly IRepository<Voies> _voieRepository;
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

    [ObservableProperty]
    private ObservableCollection<Formes> _formes = new();

    [ObservableProperty]
    private ObservableCollection<Presents> _presents = new();

    [ObservableProperty]
    private ObservableCollection<Voies> _voies = new();

    // Erreurs de validation
    [ObservableProperty]
    private bool _hasError;

    // NOTE: ErrorMessage is inherited from ViewModelBase — do NOT redeclare it here.
    // ExecuteAsync's catch block sets ViewModelBase.ErrorMessage, and the XAML reads it.

    public MedicEditViewModel(
        IRepository<Medic> repository,
        IRepository<Families> familyRepository,
        IRepository<Labos> laboRepository,
        IRepository<Dci> dciRepository,
        IRepository<Formes> formeRepository,
        IRepository<Presents> presentRepository,
        IRepository<Voies> voieRepository,
        INavigationService navigationService,
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

            var formes = await _formeRepository.GetAllAsync();
            Formes = new ObservableCollection<Formes>(formes);

            var presents = await _presentRepository.GetAllAsync();
            Presents = new ObservableCollection<Presents>(presents);

            var voies = await _voieRepository.GetAllAsync();
            Voies = new ObservableCollection<Voies>(voies);
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

        try
        {
            // Rebuild combined DCI display field from individual dci1..dci4
            Medic.dci = string.Join(" + ", new[] { Medic.dci1, Medic.dci2, Medic.dci3, Medic.dci4 }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim()));

            // Set timestamps
            var now = DateTime.Now;
            if (IsEditMode && _medicId.HasValue)
            {
                Medic.updatedat = now;
                await _repository.UpdateAsync(Medic);
            }
            else
            {
                Medic.addedat = now;
                await _repository.AddAsync(Medic);
            }

            // Synchroniser les tables de référence (DCI, Familles, Labos, Formes, Voies, Présentations)
            try { await _syncService.SyncLookupTablesAsync(Medic); } catch { /* non-fatal */ }

            // Notifier tous les ViewModels que les données ont changé
            WeakReferenceMessenger.Default.Send(new DataChangedMessage(
                new DataChangeInfo("Medic",
                    IsEditMode ? ChangeOperation.Updated : ChangeOperation.Created,
                    Medic.recordid)));

            await _dialogService.ShowSuccessAsync("Succès", 
                IsEditMode ? "Médicament mis à jour avec succès." : "Médicament créé avec succès.");
            
            _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            // Unwrap nested exceptions to find the real DB error
            var innermost = ex;
            while (innermost.InnerException != null)
                innermost = innermost.InnerException;

            var message = innermost.Message;
            HasError = true;
            ErrorMessage = message;
            await _dialogService.ShowErrorAsync("Erreur de sauvegarde", message);
        }
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
