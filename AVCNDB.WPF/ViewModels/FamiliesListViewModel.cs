using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.ViewModels;

/// <summary>
/// ViewModel pour la liste des Familles Thérapeutiques
/// </summary>
public partial class FamiliesListViewModel : ViewModelBase
{
    private readonly IRepository<Families> _repository;
    private readonly IDialogService _dialogService;

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
        IDialogService dialogService)
    {
        _repository = repository;
        _dialogService = dialogService;

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
    private void AddNew()
    {
        SelectedFamily = null;
        EditItemName = string.Empty;
        EditSubValue = string.Empty;
        IsEditing = true;
    }

    [RelayCommand]
    private void Edit()
    {
        if (SelectedFamily != null)
        {
            EditItemName = SelectedFamily.itemname;
            EditSubValue = SelectedFamily.subvalue;
            IsEditing = true;
        }
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
                SelectedFamily.itemname = EditItemName;
                SelectedFamily.subvalue = EditSubValue;
                await _repository.UpdateAsync(SelectedFamily);
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
    private async Task DeleteAsync()
    {
        if (SelectedFamily == null) return;

        if (await _dialogService.ShowConfirmAsync("Confirmer", 
            $"Supprimer la famille '{SelectedFamily.itemname}' ?"))
        {
            await ExecuteAsync(async () =>
            {
                await _repository.DeleteAsync(SelectedFamily);
                await LoadDataAsync();
            });
        }
    }
}
