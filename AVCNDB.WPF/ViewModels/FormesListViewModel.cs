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

public partial class FormesListViewModel : ViewModelBase
{
    private readonly IRepository<Formes> _repository;
    private readonly IDialogService _dialogService;
    private readonly MedicSyncService _syncService;

    [ObservableProperty]
    private ObservableCollection<Formes> _formes = new();

    [ObservableProperty]
    private Formes? _selectedForme;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editItemName = string.Empty;

    [ObservableProperty]
    private string _editSubValue = string.Empty;

    [ObservableProperty]
    private string _editFormGroup = string.Empty;

    [ObservableProperty]
    private string _editAbName = string.Empty;

    /// <summary>
    /// Abréviation libre utilisée par le dialogue Médicament pour générer
    /// la dénomination de posologie (« appl. x 3 / jour » au lieu de
    /// « Pommade x 3 / jour »).
    /// </summary>
    [ObservableProperty]
    private string _editPosoForm = string.Empty;

    private string? _originalItemName;

    public FormesListViewModel(
        IRepository<Formes> repository,
        IDialogService dialogService,
        MedicSyncService syncService)
    {
        _repository = repository;
        _dialogService = dialogService;
        _syncService = syncService;

        _ = LoadDataAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        DebounceSearch(LoadDataAsync);
    }

    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            var items = string.IsNullOrWhiteSpace(SearchText)
                ? await _repository.GetAllAsync()
                : await _repository.FindAsync(f =>
                    f.itemname.Contains(SearchText) ||
                    (f.abname != null && f.abname.Contains(SearchText)));

            Formes = new ObservableCollection<Formes>(items.OrderBy(f => f.itemname));
        }, "Chargement des formes...");
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadDataAsync();

    [RelayCommand]
    private void AddNew()
    {
        SelectedForme = null;
        EditItemName = string.Empty;
        EditSubValue = string.Empty;
        EditFormGroup = string.Empty;
        EditAbName = string.Empty;
        EditPosoForm = string.Empty;
        IsEditing = true;
    }

    [RelayCommand]
    private void Edit(Formes? forme)
    {
        if (forme == null) return;

        SelectedForme = forme;
        EditItemName = forme.itemname;
        EditSubValue = forme.subvalue;
        EditFormGroup = forme.formgroup;
        EditAbName = forme.abname;
        EditPosoForm = forme.posoform ?? string.Empty;
        _originalItemName = forme.itemname;
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(EditItemName))
        {
            await _dialogService.ShowWarningAsync("Validation", "Le nom de la forme est obligatoire.");
            return;
        }

        // Anti-doublon sur création : comparaison canonique (insensible à la casse
        // et aux accents) — empêche la coexistence de « comprimé » et « Comprimé ».
        if (SelectedForme == null)
        {
            var all = await _repository.GetAllAsync();
            if (all.Any(f => NameNormalizer.AreSame(f.itemname, EditItemName)))
            {
                await _dialogService.ShowWarningAsync("Doublon",
                    $"« {EditItemName.Trim()} » existe déjà (comparaison insensible à la casse et aux accents).");
                return;
            }
        }

        // Confirmation popup — every save in a dialog with ComboBoxes must be confirmed
        var confirmTitle = SelectedForme != null ? "Confirmer la modification" : "Confirmer la création";
        var confirmMsg = SelectedForme != null
            ? $"Enregistrer les modifications apportées à la forme\n« {EditItemName} » ?"
            : $"Créer la nouvelle forme\n« {EditItemName} » ?";
        if (!await _dialogService.ShowConfirmAsync(confirmTitle, confirmMsg)) return;

        await ExecuteAsync(async () =>
        {
            if (SelectedForme != null)
            {
                var oldName = _originalItemName;
                SelectedForme.itemname = EditItemName;
                SelectedForme.subvalue = EditSubValue;
                SelectedForme.formgroup = EditFormGroup;
                SelectedForme.abname = EditAbName;
                SelectedForme.posoform = string.IsNullOrWhiteSpace(EditPosoForm) ? null : EditPosoForm.Trim();
                await _repository.UpdateAsync(SelectedForme);

                // Propagate rename to medic.forme field
                if (!string.IsNullOrEmpty(oldName) && oldName != EditItemName)
                {
                    var updated = await _syncService.RenameFormeInMedicsAsync(oldName, EditItemName);
                    if (updated > 0)
                    {
                        await _dialogService.ShowSuccessAsync("Synchronisation",
                            $"Forme renommée dans {updated} médicament(s).");
                    }
                }
            }
            else
            {
                await _repository.AddAsync(new Formes
                {
                    itemname = EditItemName,
                    subvalue = EditSubValue,
                    formgroup = EditFormGroup,
                    abname = EditAbName,
                    posoform = string.IsNullOrWhiteSpace(EditPosoForm) ? null : EditPosoForm.Trim()
                });
            }

            _originalItemName = null;
            IsEditing = false;
            await LoadDataAsync();
            WeakReferenceMessenger.Default.Send(new DataChangedMessage(
                new DataChangeInfo("Formes", SelectedForme != null ? ChangeOperation.Renamed : ChangeOperation.Created)));
            await _dialogService.ShowSuccessAsync("Succès", "Forme sauvegardée avec succès.");
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
    private async Task DeleteAsync(Formes? forme)
    {
        if (forme == null) return;

        var usageCount = await _syncService.CountMedicsUsingFormeAsync(forme.itemname);
        var message = usageCount > 0
            ? $"La forme '{forme.itemname}' est utilisée par {usageCount} médicament(s).\nLa supprimer effacera cette référence dans tous ces médicaments.\n\nContinuer ?"
            : $"Supprimer la forme '{forme.itemname}' ?";

        if (await _dialogService.ShowConfirmAsync("Confirmer la suppression", message))
        {
            await ExecuteAsync(async () =>
            {
                if (usageCount > 0)
                {
                    await _syncService.ClearFormeFromMedicsAsync(forme.itemname);
                }
                await _repository.DeleteAsync(forme);
                await LoadDataAsync();
                WeakReferenceMessenger.Default.Send(new DataChangedMessage(
                    new DataChangeInfo("Formes", ChangeOperation.Deleted)));
            });
        }
    }

    [RelayCommand]
    private async Task ApplySubstitutsAsync()
    {
        var confirm = await _dialogService.ShowConfirmAsync(
            "Appliquer les substituts",
            "Cette action remplacera chaque dénomination Forme par son substitut dans Formes et Médicaments. Continuer ?");

        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            var result = await _syncService.ApplyFormesSubstitutsAsync();
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
                $"Formes mises à jour: {result.UpdatedEntityCount}\n" +
                $"Médicaments synchronisés: {result.UpdatedMedicCount}");
        }, "Application des substituts...");
    }
}
