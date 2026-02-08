using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Models;
using AVCNDB.WPF.Services;

namespace AVCNDB.WPF.ViewModels;

/// <summary>
/// ViewModel pour la liste des DCI (Substances Actives)
/// </summary>
public partial class DciListViewModel : ViewModelBase
{
    private readonly IRepository<Dci> _repository;
    private readonly IDialogService _dialogService;
    private readonly IExcelService _excelService;
    private readonly IStrictExcelSyncService<Dci> _strictExcelSyncService;
    private readonly MedicSyncService _syncService;

    private string? _editOldName;

    [ObservableProperty]
    private ObservableCollection<Dci> _dcis = new();

    [ObservableProperty]
    private Dci? _selectedDci;

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

    // Formulaire d'édition intégré
    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editItemName = string.Empty;

    [ObservableProperty]
    private string _editSubValue = string.Empty;

    [ObservableProperty]
    private string _editItemInfo = string.Empty;

    public DciListViewModel(
        IRepository<Dci> repository,
        IDialogService dialogService,
        IExcelService excelService,
        IStrictExcelSyncService<Dci> strictExcelSyncService,
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
        CurrentPage = 1;
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            var result = await _repository.GetPagedAsync(
                CurrentPage,
                PageSize,
                d => string.IsNullOrEmpty(SearchText) || 
                     d.itemname.Contains(SearchText),
                d => d.itemname
            );

            Dcis = new ObservableCollection<Dci>(result.Items);
            TotalCount = result.TotalCount;
            TotalPages = result.TotalPages;
        }, "Chargement des DCI...");
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
    private void AddNew()
    {
        SelectedDci = null;
        EditItemName = string.Empty;
        EditSubValue = string.Empty;
        EditItemInfo = string.Empty;
        IsEditing = true;
    }

    [RelayCommand]
    private void Edit(Dci? dci)
    {
        if (dci == null) return;

        SelectedDci = dci;
        _editOldName = dci.itemname;
        EditItemName = dci.itemname;
        EditSubValue = dci.subvalue;
        EditItemInfo = dci.iteminfo;
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(EditItemName))
        {
            await _dialogService.ShowWarningAsync("Validation", "Le nom de la DCI est obligatoire.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            if (SelectedDci != null)
            {
                // Mise à jour
                var oldName = _editOldName;
                SelectedDci.itemname = EditItemName;
                SelectedDci.subvalue = EditSubValue;
                SelectedDci.iteminfo = EditItemInfo;
                await _repository.UpdateAsync(SelectedDci);

                // Propager le renommage aux médicaments
                if (!string.IsNullOrEmpty(oldName) && oldName != EditItemName)
                {
                    var count = await _syncService.RenameDciInMedicsAsync(oldName, EditItemName);
                    if (count > 0)
                        await _dialogService.ShowSuccessAsync("Synchronisation", $"DCI renommée dans {count} médicament(s).");
                }
            }
            else
            {
                // Création
                var newDci = new Dci
                {
                    itemname = EditItemName,
                    subvalue = EditSubValue,
                    iteminfo = EditItemInfo
                };
                await _repository.AddAsync(newDci);
            }

            IsEditing = false;
            await LoadDataAsync();
            await _dialogService.ShowSuccessAsync("Succès", "DCI sauvegardée avec succès.");
        }, "Sauvegarde...");
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
    }

    [RelayCommand]
    private async Task DeleteAsync(Dci? dci)
    {
        if (dci == null) return;

        SelectedDci = dci;

        // Vérifier l'utilisation dans les médicaments
        var usageCount = await _syncService.CountMedicsUsingDciAsync(dci.itemname);
        var message = usageCount > 0
            ? $"Voulez-vous vraiment supprimer la DCI '{dci.itemname}' ?\n\n⚠️ Cette DCI est utilisée dans {usageCount} médicament(s). Les références seront effacées."
            : $"Voulez-vous vraiment supprimer la DCI '{dci.itemname}' ?";

        var confirm = await _dialogService.ShowConfirmAsync(
            "Confirmer la suppression", message);

        if (confirm)
        {
            await ExecuteAsync(async () =>
            {
                // Effacer les références dans les médicaments avant la suppression
                if (usageCount > 0)
                    await _syncService.ClearDciFromMedicsAsync(dci.itemname);

                await _repository.DeleteAsync(dci);
                await LoadDataAsync();
                await _dialogService.ShowSuccessAsync("Succès", "DCI supprimée avec succès.");
            });
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        var filePath = _dialogService.ShowSaveFileDialog(
            "Excel Files|*.xlsx",
            $"DCI_{DateTime.Now:yyyyMMdd}",
            "Exporter les DCI");

        if (!string.IsNullOrEmpty(filePath))
        {
            await ExecuteAsync(async () =>
            {
                var allDcis = await _repository.GetAllAsync();
                await _excelService.ExportAsync(allDcis, filePath, "DCI");
                await _dialogService.ShowSuccessAsync("Export réussi", $"Données exportées vers {filePath}");
            }, "Export en cours...");
        }
    }

    [RelayCommand]
    private async Task DownloadExcelTemplateAsync()
    {
        var filePath = _dialogService.ShowSaveFileDialog(
            "Excel Files|*.xlsx",
            $"DCI_Template_{DateTime.Now:yyyyMMdd}",
            "Télécharger le modèle Excel");

        if (!string.IsNullOrEmpty(filePath))
        {
            await ExecuteAsync(async () =>
            {
                await _strictExcelSyncService.CreateTemplateAsync(filePath, "DCI");
                await _dialogService.ShowSuccessAsync(
                    "Modèle généré",
                    $"Modèle Excel créé : {filePath}\nNe modifiez pas les en-têtes de colonnes.");
            }, "Génération du modèle...");
        }
    }

    [RelayCommand]
    private async Task ImportFromExcelAsync()
    {
        var filePath = _dialogService.ShowOpenFileDialog(
            "Excel Files|*.xlsx;*.xls",
            "Importer les DCI depuis Excel");

        if (string.IsNullOrEmpty(filePath)) return;

        await ExecuteAsync(async () =>
        {
            var result = await _strictExcelSyncService.ImportAndSyncAsync(filePath, "DCI");

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

    [RelayCommand]
    private void NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            _ = LoadDataAsync();
        }
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            _ = LoadDataAsync();
        }
    }
}
