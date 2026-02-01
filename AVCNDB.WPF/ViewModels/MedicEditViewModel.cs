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
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IValidationService _validationService;

    private int? _medicId;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string _pageTitle = "Nouveau Médicament";

    // Champs du formulaire
    [ObservableProperty]
    private string _itemName = string.Empty;

    [ObservableProperty]
    private string _shortName = string.Empty;

    [ObservableProperty]
    private string _barcode = string.Empty;

    [ObservableProperty]
    private string _amm = string.Empty;

    [ObservableProperty]
    private string _dci = string.Empty;

    [ObservableProperty]
    private string _forme = string.Empty;

    [ObservableProperty]
    private string _voie = string.Empty;

    [ObservableProperty]
    private string _present = string.Empty;

    [ObservableProperty]
    private string _labo = string.Empty;

    [ObservableProperty]
    private string _family = string.Empty;

    [ObservableProperty]
    private int _price;

    [ObservableProperty]
    private string _posology = string.Empty;

    [ObservableProperty]
    private string _indication = string.Empty;

    [ObservableProperty]
    private bool _isPediatric;

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private string _veic = string.Empty;

    [ObservableProperty]
    private string _tableau = string.Empty;

    // Erreurs de validation par champ
    [ObservableProperty]
    private string? _itemNameError;

    [ObservableProperty]
    private string? _barcodeError;

    public MedicEditViewModel(
        IRepository<Medic> repository,
        INavigationService navigationService,
        IDialogService dialogService,
        IValidationService validationService)
    {
        _repository = repository;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _validationService = validationService;
    }

    public override void OnNavigatedTo(object? parameter)
    {
        if (parameter is int medicId)
        {
            _medicId = medicId;
            IsEditMode = true;
            PageTitle = "Modifier le Médicament";
            _ = LoadMedicAsync(medicId);
        }
        else
        {
            _medicId = null;
            IsEditMode = false;
            PageTitle = "Nouveau Médicament";
            ClearForm();
        }
    }

    public override void OnNavigatedFrom() { }

    private async Task LoadMedicAsync(int medicId)
    {
        await ExecuteAsync(async () =>
        {
            var medic = await _repository.GetByIdAsync(medicId);
            if (medic != null)
            {
                MapMedicToForm(medic);
            }
        }, "Chargement...");
    }

    private void MapMedicToForm(Medic medic)
    {
        ItemName = medic.itemname;
        ShortName = medic.shortname;
        Barcode = medic.barcode;
        Amm = medic.amm;
        Dci = medic.dci;
        Forme = medic.forme;
        Voie = medic.voie;
        Present = medic.present;
        Labo = medic.labo;
        Family = medic.family;
        Price = medic.price;
        Posology = medic.posology;
        Indication = medic.indication;
        IsPediatric = medic.pediatric == 1;
        IsActive = medic.isactive == 1;
        Veic = medic.veic;
        Tableau = medic.tableau;
    }

    private Medic MapFormToMedic(Medic? existing = null)
    {
        var medic = existing ?? new Medic();
        
        medic.itemname = ItemName;
        medic.shortname = ShortName;
        medic.barcode = Barcode;
        medic.amm = Amm;
        medic.dci = Dci;
        medic.forme = Forme;
        medic.voie = Voie;
        medic.present = Present;
        medic.labo = Labo;
        medic.family = Family;
        medic.price = Price;
        medic.posology = Posology;
        medic.indication = Indication;
        medic.pediatric = IsPediatric ? 1 : 0;
        medic.isactive = IsActive ? 1 : 0;
        medic.veic = Veic;
        medic.tableau = Tableau;
        
        return medic;
    }

    private void ClearForm()
    {
        ItemName = string.Empty;
        ShortName = string.Empty;
        Barcode = string.Empty;
        Amm = string.Empty;
        Dci = string.Empty;
        Forme = string.Empty;
        Voie = string.Empty;
        Present = string.Empty;
        Labo = string.Empty;
        Family = string.Empty;
        Price = 0;
        Posology = string.Empty;
        Indication = string.Empty;
        IsPediatric = false;
        IsActive = true;
        Veic = string.Empty;
        Tableau = string.Empty;
        ClearErrors();
    }

    private void ClearErrors()
    {
        ItemNameError = null;
        BarcodeError = null;
    }

    private bool Validate()
    {
        ClearErrors();
        var isValid = true;

        if (string.IsNullOrWhiteSpace(ItemName))
        {
            ItemNameError = "Le nom du médicament est obligatoire";
            isValid = false;
        }

        if (!string.IsNullOrWhiteSpace(Barcode) && !_validationService.IsValidBarcode(Barcode))
        {
            BarcodeError = "Format de code-barres invalide";
            isValid = false;
        }

        return isValid;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!Validate())
        {
            await _dialogService.ShowWarningAsync("Validation", 
                "Veuillez corriger les erreurs avant de sauvegarder.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            if (IsEditMode && _medicId.HasValue)
            {
                var existing = await _repository.GetByIdAsync(_medicId.Value);
                if (existing != null)
                {
                    var updated = MapFormToMedic(existing);
                    await _repository.UpdateAsync(updated);
                }
            }
            else
            {
                var newMedic = MapFormToMedic();
                await _repository.AddAsync(newMedic);
            }

            await _dialogService.ShowSuccessAsync("Succès", 
                IsEditMode ? "Médicament mis à jour avec succès." : "Médicament créé avec succès.");
            
            _navigationService.GoBack();
        }, "Sauvegarde en cours...");
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        var hasChanges = !string.IsNullOrEmpty(ItemName) || !string.IsNullOrEmpty(Dci);
        
        if (hasChanges)
        {
            var confirm = await _dialogService.ShowConfirmAsync(
                "Annuler les modifications",
                "Voulez-vous vraiment annuler ? Les modifications non sauvegardées seront perdues.");
            
            if (!confirm) return;
        }

        _navigationService.GoBack();
    }
}
