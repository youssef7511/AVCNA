using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Helpers;
using AVCNDB.WPF.Models;
using AVCNDB.WPF.Services;

namespace AVCNDB.WPF.ViewModels;

public partial class SpecialitesListViewModel : ViewModelBase
{
    private readonly IRepository<Specialites> _repository;
    private readonly IDialogService _dialogService;
    private readonly IExcelService _excelService;
    private readonly IStrictExcelSyncService<Specialites> _strictExcelSyncService;
    private readonly MedicSyncService _syncService;

    [ObservableProperty]
    private ObservableCollection<Specialites> _specialites = new();

    [ObservableProperty]
    private Specialites? _selectedSpecialite;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editItemName = string.Empty;

    [ObservableProperty]
    private string _editAbName = string.Empty;

    [ObservableProperty]
    private string _editSubValue = string.Empty;

    public SpecialitesListViewModel(
        IRepository<Specialites> repository,
        IDialogService dialogService,
        IExcelService excelService,
        IStrictExcelSyncService<Specialites> strictExcelSyncService,
        MedicSyncService syncService)
    {
        _repository = repository;
        _dialogService = dialogService;
        _excelService = excelService;
        _strictExcelSyncService = strictExcelSyncService;
        _syncService = syncService;
        _ = LoadDataAsync();
    }

    partial void OnSearchTextChanged(string value) => DebounceSearch(LoadDataAsync);

    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            var items = string.IsNullOrWhiteSpace(SearchText)
                ? await _repository.GetAllAsync()
                : await _repository.FindAsync(s =>
                    s.itemname.Contains(SearchText) ||
                    s.abname.Contains(SearchText) ||
                    s.subvalue.Contains(SearchText));

            Specialites = new ObservableCollection<Specialites>(items.OrderBy(s => s.itemname));
        }, "Chargement des spécialités...");
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadDataAsync();

    [RelayCommand]
    private async Task SearchAsync() => await LoadDataAsync();

    [RelayCommand]
    private void AddNew()
    {
        SelectedSpecialite = null;
        EditItemName = string.Empty;
        EditAbName = string.Empty;
        EditSubValue = string.Empty;
        IsEditing = true;
    }

    [RelayCommand]
    private void Edit(Specialites? specialite)
    {
        if (specialite == null) return;

        SelectedSpecialite = specialite;
        EditItemName = specialite.itemname;
        EditAbName = specialite.abname;
        EditSubValue = specialite.subvalue;
        IsEditing = true;
    }

    [RelayCommand]
    private async Task CancelEdit()
    {
        var confirmed = await _dialogService.ShowConfirmAsync(
            "Annuler les modifications",
            "Annuler les modifications en cours ?\nLes données saisies ne seront pas enregistrées.");
        if (confirmed) IsEditing = false;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(EditItemName))
        {
            await _dialogService.ShowWarningAsync("Validation", "itemname est obligatoire.");
            return;
        }

        // Anti-doublon sur création : comparaison canonique (insensible à la casse
        // et aux accents).
        if (SelectedSpecialite == null)
        {
            var all = await _repository.GetAllAsync();
            if (all.Any(s => NameNormalizer.AreSame(s.itemname, EditItemName)))
            {
                await _dialogService.ShowWarningAsync("Doublon",
                    $"« {EditItemName.Trim()} » existe déjà (comparaison insensible à la casse et aux accents).");
                return;
            }
        }

        await ExecuteAsync(async () =>
        {
            if (SelectedSpecialite != null)
            {
                SelectedSpecialite.itemname = EditItemName;
                SelectedSpecialite.abname = EditAbName;
                SelectedSpecialite.subvalue = EditSubValue;
                await _repository.UpdateAsync(SelectedSpecialite);
            }
            else
            {
                await _repository.AddAsync(new Specialites
                {
                    itemname = EditItemName,
                    abname = EditAbName,
                    subvalue = EditSubValue
                });
            }

            IsEditing = false;
            await LoadDataAsync();
            await _dialogService.ShowSuccessAsync("Succès", "Spécialité sauvegardée avec succès.");
        }, "Sauvegarde...");
    }

    [RelayCommand]
    private async Task DeleteAsync(Specialites? specialite)
    {
        if (specialite == null) return;

        var confirm = await _dialogService.ShowConfirmAsync(
            "Confirmer la suppression",
            $"Supprimer '{specialite.itemname}' ?");

        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            await _repository.DeleteAsync(specialite);
            await LoadDataAsync();
        }, "Suppression...");
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        var filePath = _dialogService.ShowSaveFileDialog(
            "Excel Files|*.xlsx",
            $"Specialites_{DateTime.Now:yyyyMMdd}",
            "Exporter les specialites");

        if (string.IsNullOrEmpty(filePath)) return;

        await ExecuteAsync(async () =>
        {
            var checkedItems = Specialites.Where(s => s.IsChecked).ToList();
            IEnumerable<Specialites> dataToExport;
            string exportInfo;

            if (checkedItems.Count > 0)
            {
                dataToExport = checkedItems;
                exportInfo = $"{checkedItems.Count} element(s) selectionne(s) exporte(s)";
            }
            else
            {
                dataToExport = await _repository.GetAllAsync();
                exportInfo = $"{dataToExport.Count()} element(s) exporte(s)";
            }

            await _excelService.ExportAsync(dataToExport, filePath, "Specialites");
            await _dialogService.ShowSuccessAsync("Export reussi", $"{exportInfo}\n{filePath}");
        }, "Export en cours...");
    }

    [RelayCommand]
    private async Task DownloadExcelTemplateAsync()
    {
        var filePath = _dialogService.ShowSaveFileDialog(
            "Excel Files|*.xlsx",
            $"Specialites_Template_{DateTime.Now:yyyyMMdd}",
            "Telecharger le modele Excel");

        if (string.IsNullOrEmpty(filePath)) return;

        await ExecuteAsync(async () =>
        {
            await _strictExcelSyncService.CreateTemplateAsync(filePath, "Specialites");
            await _dialogService.ShowSuccessAsync(
                "Modele genere",
                $"Modele Excel cree : {filePath}\nNe modifiez pas les en-tetes de colonnes.");
        }, "Generation du modele...");
    }

    [RelayCommand]
    private async Task ImportFromExcelAsync()
    {
        var filePath = _dialogService.ShowOpenFileDialog(
            "Excel Files|*.xlsx;*.xls",
            "Importer les specialites depuis Excel");

        if (string.IsNullOrEmpty(filePath)) return;

        await ExecuteAsync(async () =>
        {
            var result = await _strictExcelSyncService.ImportAndSyncAsync(filePath, "Specialites");

            if (!result.IsValid)
            {
                await _dialogService.ShowErrorAsync("Erreur de validation", string.Join("\n", result.Errors));
                return;
            }

            await LoadDataAsync();
            await _dialogService.ShowSuccessAsync(
                "Import Excel termine",
                $"Lignes lues : {result.RowCount}\nInseres : {result.InsertedCount}\nMis a jour : {result.UpdatedCount}\nIgnores : {result.SkippedCount}");
        }, "Import en cours...");
    }

    [RelayCommand]
    private async Task ApplySubstitutsAsync()
    {
        var confirm = await _dialogService.ShowConfirmAsync(
            "Appliquer les substituts",
            "Cette action remplacera chaque dénomination Spécialité par son substitut dans Spécialités et Médicaments. Continuer ?");

        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            var result = await _syncService.ApplySpecialitesSubstitutsAsync();
            await LoadDataAsync();

            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                await _dialogService.ShowErrorAsync(
                    "Application des substituts échouée",
                    $"Erreur: {result.ErrorMessage}");
                return;
            }

            await _dialogService.ShowSuccessAsync(
                "Substituts appliqués",
                $"Candidats: {result.CandidateCount}\n" +
                $"Remplacements appliqués: {result.AppliedCount}\n" +
                $"Spécialités mises à jour: {result.UpdatedEntityCount}\n" +
                $"Médicaments synchronisés: {result.UpdatedMedicCount}");
        }, "Application des substituts...");
    }
}
