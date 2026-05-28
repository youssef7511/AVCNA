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

    // Formulaire d'édition intégré
    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editItemName = string.Empty;

    [ObservableProperty]
    private string _editSubValue = string.Empty;

    [ObservableProperty]
    private string _editItemInfo = string.Empty;

    [ObservableProperty]
    private bool _isDetailsOpen;

    [ObservableProperty]
    private string _detailItemName = string.Empty;

    [ObservableProperty]
    private string _detailSubValue = string.Empty;

    [ObservableProperty]
    private string _detailItemInfo = string.Empty;

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
        DebounceSearch(LoadDataAsync);
    }

    partial void OnSelectedDciChanged(Dci? value)
    {
        if (value == null)
        {
            IsDetailsOpen = false;
        }
    }

    partial void OnIsEditingChanged(bool value)
    {
        if (value)
        {
            IsDetailsOpen = false;
        }
    }

    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Chargement complet (pas de pagination) : la grille DCI se comporte
            // comme les autres tables de référence — la DataGrid virtualise et
            // défile, donc toute DCI (y compris une nouvelle) reste atteignable.
            var items = string.IsNullOrWhiteSpace(SearchText)
                ? await _repository.GetAllAsync()
                : await _repository.FindAsync(d => d.itemname.Contains(SearchText));

            Dcis = new ObservableCollection<Dci>(items.OrderBy(d => d.itemname));
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
        var normalizedItemName = EditItemName?.Trim() ?? string.Empty;
        var normalizedSubValue = EditSubValue?.Trim() ?? string.Empty;
        var normalizedItemInfo = EditItemInfo?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedItemName))
        {
            await _dialogService.ShowWarningAsync("Validation", "Le nom de la DCI est obligatoire.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            if (SelectedDci != null)
            {
                // Mise a jour robuste via rechargement en base (evite les soucis d'entite detachee)
                var dciToUpdate = await _repository.GetByIdAsync(SelectedDci.recordid);
                if (dciToUpdate == null)
                {
                    await _dialogService.ShowErrorAsync("Erreur", "La DCI a modifier est introuvable.");
                    return;
                }

                var oldName = dciToUpdate.itemname;
                dciToUpdate.itemname = normalizedItemName;
                dciToUpdate.subvalue = normalizedSubValue;
                dciToUpdate.iteminfo = normalizedItemInfo;
                await _repository.UpdateAsync(dciToUpdate);

                // Propager le renommage aux medicaments
                if (!string.IsNullOrEmpty(oldName) &&
                    !string.Equals(oldName, normalizedItemName, StringComparison.Ordinal))
                {
                    var medicCount = await _syncService.RenameDciInMedicsAsync(oldName, normalizedItemName);
                    var interactCount = await _syncService.RenameDciInInteractionsAsync(oldName, normalizedItemName);
                    if (medicCount > 0 || interactCount > 0)
                    {
                        await _dialogService.ShowSuccessAsync(
                            "Synchronisation",
                            $"Remplacement propage: {medicCount} medicament(s), {interactCount} interaction(s).");
                    }
                }
            }
            else
            {
                // Création — une dénomination composée « X+Y » est scindée en DCI
                // distinctes (règle métier : le « + » sépare deux principes actifs).
                // On ne conserve QUE les segments séparés (jamais la ligne composée),
                // et on ignore ceux qui existent déjà (comparaison canonique).
                var segments = DciCompositeSplitter.Split(normalizedItemName);
                var existing = await _repository.GetAllAsync();
                var existingCanon = new HashSet<string>(
                    existing.Select(d => NameNormalizer.Canonical(d.itemname)));

                var addedCount = 0;
                foreach (var seg in segments)
                {
                    if (existingCanon.Contains(NameNormalizer.Canonical(seg))) continue;
                    await _repository.AddAsync(new Dci
                    {
                        itemname = seg,
                        // subvalue / iteminfo ne s'appliquent qu'à une DCI simple ;
                        // sur une saisie composée, on ne les recopie pas sur chaque segment.
                        subvalue = segments.Count == 1 ? normalizedSubValue : string.Empty,
                        iteminfo = segments.Count == 1 ? normalizedItemInfo : string.Empty
                    });
                    existingCanon.Add(NameNormalizer.Canonical(seg));
                    addedCount++;
                }

                if (addedCount == 0)
                {
                    await _dialogService.ShowWarningAsync(
                        "Aucun ajout",
                        "Toutes les DCI saisies existent déjà dans le référentiel.");
                    // Rester dans le dialogue d'ajout (comme les autres tables de
                    // référence) : ne pas fermer le formulaire ni afficher « Succès ».
                    return;
                }
            }

            IsEditing = false;

            // Effacer un éventuel filtre de recherche pour que le nouvel item
            // apparaisse dans la liste complète (la grille charge désormais tout).
            SearchText = string.Empty;

            await LoadDataAsync();
            WeakReferenceMessenger.Default.Send(new DataChangedMessage(
                new DataChangeInfo("Dci", SelectedDci != null ? ChangeOperation.Renamed : ChangeOperation.Created)));
            await _dialogService.ShowSuccessAsync("Succes", "DCI sauvegardee avec succes.");
        }, "Sauvegarde...");
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
    private void CloseDetails()
    {
        IsDetailsOpen = false;
        SelectedDci = null;
    }

    public void OpenDetails(Dci? dci)
    {
        if (dci == null || IsEditing)
        {
            return;
        }

        DetailItemName = dci.itemname;
        DetailSubValue = dci.subvalue;
        DetailItemInfo = dci.iteminfo;
        IsDetailsOpen = true;
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

                await _syncService.ClearDciFromInteractionsAsync(dci.itemname);

                await _repository.DeleteAsync(dci);
                await LoadDataAsync();
                WeakReferenceMessenger.Default.Send(new DataChangedMessage(
                    new DataChangeInfo("Dci", ChangeOperation.Deleted)));
                await _dialogService.ShowSuccessAsync("Succès", "DCI supprimée avec succès.");
            });
        }
    }

    [RelayCommand]
    private async Task ApplySubstitutsAsync()
    {
        var confirm = await _dialogService.ShowConfirmAsync(
            "Appliquer les substituts",
            "Cette action remplacera chaque dénomination DCI par son substitut dans DCI, Médicaments et Interactions. Continuer ?");

        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            var result = await _syncService.ApplyDciSubstitutsAsync();
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
                $"Lignes DCI mises à jour: {result.UpdatedDciCount}\n" +
                $"Médicaments synchronisés: {result.UpdatedMedicCount}\n" +
                $"Interactions synchronisées: {result.UpdatedInteractCount}");
        }, "Application des substituts...");
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
                var checkedItems = Dcis.Where(d => d.IsChecked).ToList();
                IEnumerable<Dci> dataToExport;
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

                await _excelService.ExportAsync(dataToExport, filePath, "DCI");
                await _dialogService.ShowSuccessAsync("Export réussi", $"{exportInfo}\n{filePath}");
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
}
