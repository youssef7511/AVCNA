using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Models;
using AVCNDB.WPF.Services;

namespace AVCNDB.WPF.ViewModels;

/// <summary>
/// ViewModel pour la liste des Laboratoires
/// </summary>
public partial class LabosListViewModel : ViewModelBase
{
    private readonly IRepository<Labos> _repository;
    private readonly IDialogService _dialogService;
    private readonly IExcelService _excelService;
    private readonly IStrictExcelSyncService<Labos> _strictExcelSyncService;
    private readonly MedicSyncService _syncService;

    private string? _editOldName;

    [ObservableProperty]
    private ObservableCollection<Labos> _labos = new();

    [ObservableProperty]
    private Labos? _selectedLabo;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editItemName = string.Empty;

    [ObservableProperty]
    private string _editSubValue = string.Empty;

    public LabosListViewModel(
        IRepository<Labos> repository,
        IDialogService dialogService,
        IExcelService excelService,
        IStrictExcelSyncService<Labos> strictExcelSyncService,
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
                : await _repository.FindAsync(l => l.itemname.Contains(SearchText));

            Labos = new ObservableCollection<Labos>(items.OrderBy(l => l.itemname));
        }, "Chargement des laboratoires...");
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadDataAsync();

    [RelayCommand]
    private async Task SearchAsync() => await LoadDataAsync();

    [RelayCommand]
    private void AddNew()
    {
        SelectedLabo = null;
        EditItemName = string.Empty;
        EditSubValue = string.Empty;
        IsEditing = true;
    }

    [RelayCommand]
    private void Edit(Labos? labo)
    {
        if (labo == null) return;

        SelectedLabo = labo;
        _editOldName = labo.itemname;
        EditItemName = labo.itemname;
        EditSubValue = labo.subvalue;
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(EditItemName))
        {
            await _dialogService.ShowWarningAsync("Validation", "Le nom du laboratoire est obligatoire.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            if (SelectedLabo != null)
            {
                var oldName = _editOldName;
                SelectedLabo.itemname = EditItemName;
                SelectedLabo.subvalue = EditSubValue;
                await _repository.UpdateAsync(SelectedLabo);

                // Propager le renommage aux médicaments
                if (!string.IsNullOrEmpty(oldName) && oldName != EditItemName)
                {
                    var count = await _syncService.RenameLaboInMedicsAsync(oldName, EditItemName);
                    if (count > 0)
                        await _dialogService.ShowSuccessAsync("Synchronisation", $"Laboratoire renommé dans {count} médicament(s).");
                }
            }
            else
            {
                await _repository.AddAsync(new Labos
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
    private async Task DeleteAsync(Labos? labo)
    {
        if (labo == null) return;

        SelectedLabo = labo;

        // Vérifier l'utilisation dans les médicaments
        var usageCount = await _syncService.CountMedicsUsingLaboAsync(labo.itemname);
        var message = usageCount > 0
            ? $"Supprimer le laboratoire '{labo.itemname}' ?\n\n⚠️ Utilisé dans {usageCount} médicament(s). Les références seront effacées."
            : $"Supprimer le laboratoire '{labo.itemname}' ?";

        if (await _dialogService.ShowConfirmAsync("Confirmer", message))
        {
            await ExecuteAsync(async () =>
            {
                if (usageCount > 0)
                    await _syncService.ClearLaboFromMedicsAsync(labo.itemname);

                await _repository.DeleteAsync(labo);
                await LoadDataAsync();
            });
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        var filePath = _dialogService.ShowSaveFileDialog(
            "Excel Files|*.xlsx",
            $"Labos_{DateTime.Now:yyyyMMdd}",
            "Exporter les laboratoires");

        if (string.IsNullOrEmpty(filePath)) return;

        await ExecuteAsync(async () =>
        {
            var allItems = await _repository.GetAllAsync();
            await _excelService.ExportAsync(allItems, filePath, "Laboratoires");
            await _dialogService.ShowSuccessAsync("Export réussi", $"Données exportées vers {filePath}");
        }, "Export en cours...");
    }

    [RelayCommand]
    private async Task DownloadExcelTemplateAsync()
    {
        var filePath = _dialogService.ShowSaveFileDialog(
            "Excel Files|*.xlsx",
            $"Labos_Template_{DateTime.Now:yyyyMMdd}",
            "Télécharger le modèle Excel");

        if (string.IsNullOrEmpty(filePath)) return;

        await ExecuteAsync(async () =>
        {
            await _strictExcelSyncService.CreateTemplateAsync(filePath, "Laboratoires");
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
            "Importer les laboratoires depuis Excel");

        if (string.IsNullOrEmpty(filePath)) return;

        await ExecuteAsync(async () =>
        {
            var result = await _strictExcelSyncService.ImportAndSyncAsync(filePath, "Laboratoires");

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
