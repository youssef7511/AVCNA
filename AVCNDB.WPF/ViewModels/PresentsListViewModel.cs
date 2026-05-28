using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Helpers;
using AVCNDB.WPF.Messages;
using AVCNDB.WPF.Models;
using AVCNDB.WPF.Services;

namespace AVCNDB.WPF.ViewModels;

public partial class PresentsListViewModel : ViewModelBase
{
    private readonly IRepository<Presents> _repository;
    private readonly IDialogService _dialogService;
    private readonly IExcelService _excelService;
    private readonly IStrictExcelSyncService<Presents> _strictExcelSyncService;
    private readonly MedicSyncService _syncService;

    private string? _originalItemName;

    [ObservableProperty]
    private ObservableCollection<Presents> _presents = new();

    [ObservableProperty]
    private Presents? _selectedPresent;

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

    public PresentsListViewModel(
        IRepository<Presents> repository,
        IDialogService dialogService,
        IExcelService excelService,
        IStrictExcelSyncService<Presents> strictExcelSyncService,
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
                : await _repository.FindAsync(p =>
                    p.itemname.Contains(SearchText) ||
                    p.abname.Contains(SearchText) ||
                    p.subvalue.Contains(SearchText));

            Presents = new ObservableCollection<Presents>(items.OrderBy(p => p.itemname));
        }, "Chargement des présentations...");
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadDataAsync();

    [RelayCommand]
    private async Task SearchAsync() => await LoadDataAsync();

    [RelayCommand]
    private void AddNew()
    {
        SelectedPresent = null;
        EditItemName = string.Empty;
        EditAbName = string.Empty;
        EditSubValue = string.Empty;
        IsEditing = true;
    }

    [RelayCommand]
    private void Edit(Presents? present)
    {
        if (present == null) return;

        SelectedPresent = present;
        EditItemName = present.itemname;
        EditAbName = present.abname;
        EditSubValue = present.subvalue;
        _originalItemName = present.itemname;
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
        if (SelectedPresent == null)
        {
            var all = await _repository.GetAllAsync();
            if (all.Any(p => NameNormalizer.AreSame(p.itemname, EditItemName)))
            {
                await _dialogService.ShowWarningAsync("Doublon",
                    $"« {EditItemName.Trim()} » existe déjà (comparaison insensible à la casse et aux accents).");
                return;
            }
        }

        await ExecuteAsync(async () =>
        {
            if (SelectedPresent != null)
            {
                var oldName = _originalItemName;
                SelectedPresent.itemname = EditItemName;
                SelectedPresent.abname = EditAbName;
                SelectedPresent.subvalue = EditSubValue;
                await _repository.UpdateAsync(SelectedPresent);

                // Propager le renommage aux médicaments
                if (!string.IsNullOrEmpty(oldName) && oldName != EditItemName)
                {
                    var updated = await _syncService.RenamePresentInMedicsAsync(oldName, EditItemName);
                    if (updated > 0)
                    {
                        await _dialogService.ShowSuccessAsync("Synchronisation",
                            $"Présentation renommée dans {updated} médicament(s).");
                    }
                }
            }
            else
            {
                await _repository.AddAsync(new Presents
                {
                    itemname = EditItemName,
                    abname = EditAbName,
                    subvalue = EditSubValue
                });
            }

            _originalItemName = null;
            IsEditing = false;
            await LoadDataAsync();
            WeakReferenceMessenger.Default.Send(new DataChangedMessage(
                new DataChangeInfo("Presents", SelectedPresent != null ? ChangeOperation.Renamed : ChangeOperation.Created)));
            await _dialogService.ShowSuccessAsync("Succès", "Présentation sauvegardée avec succès.");
        }, "Sauvegarde...");
    }

    [RelayCommand]
    private async Task DeleteAsync(Presents? present)
    {
        if (present == null) return;

        var usageCount = await _syncService.CountMedicsUsingPresentAsync(present.itemname);
        var message = usageCount > 0
            ? $"La présentation '{present.itemname}' est utilisée par {usageCount} médicament(s).\nLa supprimer effacera cette référence dans tous ces médicaments.\n\nContinuer ?"
            : $"Supprimer la présentation '{present.itemname}' ?";

        if (await _dialogService.ShowConfirmAsync("Confirmer la suppression", message))
        {
            await ExecuteAsync(async () =>
            {
                if (usageCount > 0)
                {
                    await _syncService.ClearPresentFromMedicsAsync(present.itemname);
                }
                await _repository.DeleteAsync(present);
                await LoadDataAsync();
                WeakReferenceMessenger.Default.Send(new DataChangedMessage(
                    new DataChangeInfo("Presents", ChangeOperation.Deleted)));
            }, "Suppression...");
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        var filePath = _dialogService.ShowSaveFileDialog(
            "Excel Files|*.xlsx",
            $"Presents_{DateTime.Now:yyyyMMdd}",
            "Exporter les presents");

        if (string.IsNullOrEmpty(filePath)) return;

        await ExecuteAsync(async () =>
        {
            var checkedItems = Presents.Where(p => p.IsChecked).ToList();
            IEnumerable<Presents> dataToExport;
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

            await _excelService.ExportAsync(dataToExport, filePath, "Presents");
            await _dialogService.ShowSuccessAsync("Export reussi", $"{exportInfo}\n{filePath}");
        }, "Export en cours...");
    }

    [RelayCommand]
    private async Task DownloadExcelTemplateAsync()
    {
        var filePath = _dialogService.ShowSaveFileDialog(
            "Excel Files|*.xlsx",
            $"Presents_Template_{DateTime.Now:yyyyMMdd}",
            "Telecharger le modele Excel");

        if (string.IsNullOrEmpty(filePath)) return;

        await ExecuteAsync(async () =>
        {
            await _strictExcelSyncService.CreateTemplateAsync(filePath, "Presents");
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
            "Importer les presents depuis Excel");

        if (string.IsNullOrEmpty(filePath)) return;

        await ExecuteAsync(async () =>
        {
            var result = await _strictExcelSyncService.ImportAndSyncAsync(filePath, "Presents");

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
            "Cette action remplacera chaque dénomination Présentation par son substitut dans Présentations et Médicaments. Continuer ?");

        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            var result = await _syncService.ApplyPresentsSubstitutsAsync();
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
                $"Présentations mises à jour: {result.UpdatedEntityCount}\n" +
                $"Médicaments synchronisés: {result.UpdatedMedicCount}");
        }, "Application des substituts...");
    }
}
