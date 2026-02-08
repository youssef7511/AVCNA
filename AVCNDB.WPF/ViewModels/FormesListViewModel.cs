using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;
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

    private string? _originalItemName;

    public FormesListViewModel(IRepository<Formes> repository, IDialogService dialogService, MedicSyncService syncService)
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
                : await _repository.FindAsync(f => f.itemname.Contains(SearchText));

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
        IsEditing = true;
    }

    [RelayCommand]
    private void Edit(Formes? forme)
    {
        if (forme == null) return;

        SelectedForme = forme;
        EditItemName = forme.itemname;
        EditSubValue = forme.subvalue;
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

        await ExecuteAsync(async () =>
        {
            if (SelectedForme != null)
            {
                var oldName = _originalItemName;
                SelectedForme.itemname = EditItemName;
                SelectedForme.subvalue = EditSubValue;
                await _repository.UpdateAsync(SelectedForme);

                // Propagate rename to medic.forme field
                if (!string.IsNullOrEmpty(oldName) && oldName != EditItemName)
                {
                    var updated = await _syncService.RenameFormeInMedicsAsync(oldName, EditItemName);
                    if (updated > 0)
                    {
                        await _dialogService.ShowInfoAsync("Synchronisation",
                            $"Forme renommée dans {updated} médicament(s).");
                    }
                }
            }
            else
            {
                await _repository.AddAsync(new Formes
                {
                    itemname = EditItemName,
                    subvalue = EditSubValue
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
            });
        }
    }
}
