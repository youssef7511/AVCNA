using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Models;
using AVCNDB.WPF.Services;

namespace AVCNDB.WPF.ViewModels;

/// <summary>
/// ViewModel pour la liste des Familles Thérapeutiques
/// </summary>
public partial class FamiliesListViewModel : ViewModelBase
{
    private readonly IRepository<Families> _repository;
    private readonly IDialogService _dialogService;
    private readonly IExcelService _excelService;
    private readonly IStrictExcelSyncService<Families> _strictExcelSyncService;
    private readonly MedicSyncService _syncService;

    private string? _editOldName;

    [ObservableProperty]
    private ObservableCollection<Families> _families = new();

    [ObservableProperty]
    private Families? _selectedFamily;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editItemName = string.Empty;

    [ObservableProperty]
    private string _editSubValue = string.Empty;

    public FamiliesListViewModel(
        IRepository<Families> repository,
        IDialogService dialogService,
        IExcelService excelService,
        IStrictExcelSyncService<Families> strictExcelSyncService,
        MedicSyncService syncService)
    {
        _repository = repository;
        _dialogService = dialogService;
        _excelService = excelService;
        _strictExcelSyncService = strictExcelSyncService;
        _syncService = syncService;

        _ = LoadDataAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            var items = string.IsNullOrEmpty(SearchText)
                ? await _repository.GetAllAsync()
                : await _repository.FindAsync(f => f.itemname.Contains(SearchText));

            Families = new ObservableCollection<Families>(items.OrderBy(f => f.itemname));
        }, "Chargement des familles...");
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadDataAsync();

    [RelayCommand]
    private async Task SearchAsync() => await LoadDataAsync();

    [RelayCommand]
    private void AddNew()
    {
        SelectedFamily = null;
        EditItemName = string.Empty;
        EditSubValue = string.Empty;
        IsEditing = true;
    }

    [RelayCommand]
    private void Edit(Families? family)
    {
        if (family == null) return;

        SelectedFamily = family;
        _editOldName = family.itemname;
        EditItemName = family.itemname;
        EditSubValue = family.subvalue;
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(EditItemName))
        {
            await _dialogService.ShowWarningAsync("Validation", "Le nom de la famille est obligatoire.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            if (SelectedFamily != null)
            {
                var oldName = _editOldName;
                SelectedFamily.itemname = EditItemName;
                SelectedFamily.subvalue = EditSubValue;
                await _repository.UpdateAsync(SelectedFamily);

                // Propager le renommage aux médicaments
                if (!string.IsNullOrEmpty(oldName) && oldName != EditItemName)
                {
                    var count = await _syncService.RenameFamilyInMedicsAsync(oldName, EditItemName);
                    if (count > 0)
                        await _dialogService.ShowSuccessAsync("Synchronisation", $"Famille renommée dans {count} médicament(s).");
                }
            }
            else
            {
                await _repository.AddAsync(new Families
                {
                    itemname = EditItemName,
                    subvalue = EditSubValue
                });
            }

            IsEditing = false;
            await LoadDataAsync();
        }, "Sauvegarde...");
    }

    [RelayCommand]
    private void CancelEdit() => IsEditing = false;

    [RelayCommand]
    private async Task DeleteAsync(Families? family)
    {
        if (family == null) return;

        SelectedFamily = family;

        // Vérifier l'utilisation dans les médicaments
        var usageCount = await _syncService.CountMedicsUsingFamilyAsync(family.itemname);
        var message = usageCount > 0
            ? $"Supprimer la famille '{family.itemname}' ?\n\n⚠️ Utilisée dans {usageCount} médicament(s). Les références seront effacées."
            : $"Supprimer la famille '{family.itemname}' ?";

        if (await _dialogService.ShowConfirmAsync("Confirmer", message))
        {
            await ExecuteAsync(async () =>
            {
                if (usageCount > 0)
                    await _syncService.ClearFamilyFromMedicsAsync(family.itemname);

                await _repository.DeleteAsync(family);
                await LoadDataAsync();
            });
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        var filePath = _dialogService.ShowSaveFileDialog(
            "Excel Files|*.xlsx",
            $"Families_{DateTime.Now:yyyyMMdd}",
            "Exporter les familles");

        if (string.IsNullOrEmpty(filePath)) return;

        await ExecuteAsync(async () =>
        {
            var checkedItems = Families.Where(f => f.IsChecked).ToList();
            IEnumerable<Families> dataToExport;
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

            await _excelService.ExportAsync(dataToExport, filePath, "Familles");
            await _dialogService.ShowSuccessAsync("Export réussi", $"{exportInfo}\n{filePath}");
        }, "Export en cours...");
    }

    [RelayCommand]
    private async Task DownloadExcelTemplateAsync()
    {
        var filePath = _dialogService.ShowSaveFileDialog(
            "Excel Files|*.xlsx",
            $"Families_Template_{DateTime.Now:yyyyMMdd}",
            "Télécharger le modèle Excel");

        if (string.IsNullOrEmpty(filePath)) return;

        await ExecuteAsync(async () =>
        {
            await _strictExcelSyncService.CreateTemplateAsync(filePath, "Familles");
            await _dialogService.ShowSuccessAsync(
                "Modèle généré",
                $"Modèle Excel créé : {filePath}\nNe modifiez pas les en-têtes de colonnes.");
        }, "Génération du modèle...");
    }

    [RelayCommand]
    private async Task ImportFromExcelAsync()
    {
        var filePath = _dialogService.ShowOpenFileDialog(
            "Excel Files|*.xlsx;*.xls",
            "Importer les familles depuis Excel");

        if (string.IsNullOrEmpty(filePath)) return;

        await ExecuteAsync(async () =>
        {
            var result = await _strictExcelSyncService.ImportAndSyncAsync(filePath, "Familles");

            if (!result.IsValid)
            {
                await _dialogService.ShowErrorAsync("Erreur de validation", string.Join("\n", result.Errors));
                return;
            }

            await LoadDataAsync();
            await _dialogService.ShowSuccessAsync(
                "Import Excel terminé",
                $"Lignes lues : {result.RowCount}\nInsérés : {result.InsertedCount}\nMis à jour : {result.UpdatedCount}\nIgnorés : {result.SkippedCount}");
        }, "Import en cours...");
    }
}
