using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.ViewModels;

/// <summary>
/// ViewModel pour la liste des Laboratoires
/// </summary>
public partial class LabosListViewModel : ViewModelBase
{
    private readonly IRepository<Labos> _repository;
    private readonly IDialogService _dialogService;

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
                : await _repository.FindAsync(l => l.itemname.Contains(SearchText));

            Labos = new ObservableCollection<Labos>(items.OrderBy(l => l.itemname));
        }, "Chargement des laboratoires...");
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadDataAsync();

    [RelayCommand]
    private void AddNew()
    {
        SelectedLabo = null;
        EditItemName = string.Empty;
        EditSubValue = string.Empty;
        IsEditing = true;
    }

    [RelayCommand]
    private void Edit()
    {
        if (SelectedLabo != null)
        {
            EditItemName = SelectedLabo.itemname;
            EditSubValue = SelectedLabo.subvalue;
            IsEditing = true;
        }
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
                SelectedLabo.itemname = EditItemName;
                SelectedLabo.subvalue = EditSubValue;
                await _repository.UpdateAsync(SelectedLabo);
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
    private async Task DeleteAsync()
    {
        if (SelectedLabo == null) return;

        if (await _dialogService.ShowConfirmAsync("Confirmer", 
            $"Supprimer le laboratoire '{SelectedLabo.itemname}' ?"))
        {
            await ExecuteAsync(async () =>
            {
                await _repository.DeleteAsync(SelectedLabo);
                await LoadDataAsync();
            });
        }
    }
}
