using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.ViewModels;

public partial class PresentsListViewModel : ViewModelBase
{
    private readonly IRepository<Presents> _repository;
    private readonly IDialogService _dialogService;
    private readonly IExcelService _excelService;
    private readonly IStrictExcelSyncService<Presents> _strictExcelSyncService;

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
        IStrictExcelSyncService<Presents> strictExcelSyncService)
    {
        _repository = repository;
        _dialogService = dialogService;
        _excelService = excelService;
        _strictExcelSyncService = strictExcelSyncService;
        _ = LoadDataAsync();
    }

    partial void OnSearchTextChanged(string value) => _ = LoadDataAsync();

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
        IsEditing = true;
    }

    [RelayCommand]
    private void CancelEdit() => IsEditing = false;

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(EditItemName))
        {
            await _dialogService.ShowWarningAsync("Validation", "itemname est obligatoire.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            if (SelectedPresent != null)
            {
                SelectedPresent.itemname = EditItemName;
                SelectedPresent.abname = EditAbName;
                SelectedPresent.subvalue = EditSubValue;
                await _repository.UpdateAsync(SelectedPresent);
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

            IsEditing = false;
            await LoadDataAsync();
        }, "Sauvegarde...");
    }

    [RelayCommand]
    private async Task DeleteAsync(Presents? present)
    {
        if (present == null) return;

        var confirm = await _dialogService.ShowConfirmAsync(
            "Confirmer la suppression",
            $"Supprimer '{present.itemname}' ?");

        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            await _repository.DeleteAsync(present);
            await LoadDataAsync();
        }, "Suppression...");
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
}
