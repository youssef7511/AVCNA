using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Models;
using AVCNDB.WPF.Services;

namespace AVCNDB.WPF.ViewModels;

public partial class VoiesListViewModel : ViewModelBase
{
    private readonly IRepository<Voies> _repository;
    private readonly IDialogService _dialogService;
    private readonly MedicSyncService _syncService;

    [ObservableProperty]
    private ObservableCollection<Voies> _voies = new();

    [ObservableProperty]
    private Voies? _selectedVoie;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editItemName = string.Empty;

    [ObservableProperty]
    private string _editSubValue = string.Empty;

    [ObservableProperty]
    private string _editAbName = string.Empty;

    private string? _originalItemName;

    public VoiesListViewModel(IRepository<Voies> repository, IDialogService dialogService, MedicSyncService syncService)
    {
        _repository = repository;
        _dialogService = dialogService;
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
            var items = string.IsNullOrWhiteSpace(SearchText)
                ? await _repository.GetAllAsync()
                : await _repository.FindAsync(v => v.itemname.Contains(SearchText));

            Voies = new ObservableCollection<Voies>(items.OrderBy(v => v.itemname));
        }, "Chargement des voies...");
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadDataAsync();

    [RelayCommand]
    private void AddNew()
    {
        SelectedVoie = null;
        EditItemName = string.Empty;
        EditSubValue = string.Empty;
        EditAbName = string.Empty;
        IsEditing = true;
    }

    [RelayCommand]
    private void Edit(Voies? voie)
    {
        if (voie == null) return;

        SelectedVoie = voie;
        EditItemName = voie.itemname;
        EditSubValue = voie.subvalue;
        EditAbName = voie.abname;
        _originalItemName = voie.itemname;
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(EditItemName))
        {
            await _dialogService.ShowWarningAsync("Validation", "Le nom de la voie est obligatoire.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            if (SelectedVoie != null)
            {
                var oldName = _originalItemName;
                SelectedVoie.itemname = EditItemName;
                SelectedVoie.subvalue = EditSubValue;
                SelectedVoie.abname = EditAbName;
                await _repository.UpdateAsync(SelectedVoie);

                // Propagate rename to medic.voie field
                if (!string.IsNullOrEmpty(oldName) && oldName != EditItemName)
                {
                    var updated = await _syncService.RenameVoieInMedicsAsync(oldName, EditItemName);
                    if (updated > 0)
                    {
                        await _dialogService.ShowInfoAsync("Synchronisation",
                            $"Voie renommée dans {updated} médicament(s).");
                    }
                }
            }
            else
            {
                await _repository.AddAsync(new Voies
                {
                    itemname = EditItemName,
                    subvalue = EditSubValue,
                    abname = EditAbName
                });
            }

            _originalItemName = null;
            IsEditing = false;
            await LoadDataAsync();
        }, "Sauvegarde...");
    }

    [RelayCommand]
    private void CancelEdit() => IsEditing = false;

    [RelayCommand]
    private async Task DeleteAsync(Voies? voie)
    {
        if (voie == null) return;

        var usageCount = await _syncService.CountMedicsUsingVoieAsync(voie.itemname);
        var message = usageCount > 0
            ? $"La voie '{voie.itemname}' est utilisée par {usageCount} médicament(s).\nLa supprimer effacera cette référence dans tous ces médicaments.\n\nContinuer ?"
            : $"Supprimer la voie '{voie.itemname}' ?";

        if (await _dialogService.ShowConfirmAsync("Confirmer la suppression", message))
        {
            await ExecuteAsync(async () =>
            {
                if (usageCount > 0)
                {
                    await _syncService.ClearVoieFromMedicsAsync(voie.itemname);
                }
                await _repository.DeleteAsync(voie);
                await LoadDataAsync();
            });
        }
    }
}
