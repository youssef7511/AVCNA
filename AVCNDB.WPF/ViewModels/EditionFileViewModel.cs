using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.ViewModels;

/// <summary>
/// ViewModel for the "Fichier d'édition" page (D-01 through D-06).
/// Orchestrates Excel import, ML detection, row approval/rejection, and display filtering.
/// </summary>
public partial class EditionFileViewModel : ViewModelBase
{
    private readonly IEditionFileService _editionFileService;
    private readonly IDialogService _dialogService;

    // ─── Source rows (all) ────────────────────────────────────────────────
    private List<EditionRowViewModel> _allRows = new();

    // ─── Displayed rows (filtered) ────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<EditionRowViewModel> _rows = new();

    [ObservableProperty]
    private EditionRowViewModel? _selectedRow;

    // ─── Import state ────────────────────────────────────────────────────
    [ObservableProperty]
    private string _currentFilePath = string.Empty;

    [ObservableProperty]
    private EditionSourceType _selectedSourceType = EditionSourceType.ExcelSimple;

    [ObservableProperty]
    private int _totalRowCount;

    [ObservableProperty]
    private int _unknownRowCount;

    [ObservableProperty]
    private int _approvedRowCount;

    // ─── Display filter ──────────────────────────────────────────────────
    /// <summary>
    /// "Tous" | "Actifs" | "Inactifs" | "Affectés" | "Non-affectés" |
    /// "Méd.[V.E.I]" | "Avec A.P" | "Sans A.P" | "Remboursés" | "Non-remboursés" |
    /// "Sélectionnés" | "Non-sélectionnés" | "Changements de prix"
    /// </summary>
    [ObservableProperty]
    private string _filterType = "Tous";

    // ─── Constructor ─────────────────────────────────────────────────────

    public EditionFileViewModel(
        IEditionFileService editionFileService,
        IDialogService dialogService)
    {
        _editionFileService = editionFileService;
        _dialogService = dialogService;
    }

    // ─── Property change handlers ─────────────────────────────────────────

    partial void OnFilterTypeChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedRowChanged(EditionRowViewModel? value)
    {
        ApproveRowCommand.NotifyCanExecuteChanged();
        RejectRowCommand.NotifyCanExecuteChanged();
        MarkAsNewCommand.NotifyCanExecuteChanged();
        MarkForDeletionCommand.NotifyCanExecuteChanged();
        ResetRowCommand.NotifyCanExecuteChanged();
        SimilaritySearchCommand.NotifyCanExecuteChanged();
    }

    // ─── Commands ─────────────────────────────────────────────────────────

    /// <summary>
    /// Open file dialog -> import Excel -> run ML detection -> populate grid
    /// </summary>
    [RelayCommand]
    private async Task ImportAsync()
    {
        var filePath = _dialogService.ShowOpenFileDialog(
            "Fichiers Excel|*.xlsx;*.xls", "Importer un fichier d'édition");

        if (string.IsNullOrEmpty(filePath))
            return;

        await ExecuteAsync(async () =>
        {
            // 1. Import Excel
            var importResult = await _editionFileService.ImportExcelAsync(filePath, SelectedSourceType);

            if (!importResult.Success)
            {
                await _dialogService.ShowErrorAsync("Erreur d'import", importResult.ErrorMessage ?? "Erreur inconnue");
                return;
            }

            // 2. Run ML detection
            var unknownCount = await _editionFileService.ValidateAgainstLibraryAsync(importResult.Rows);

            // 3. Wrap in ViewModels
            _allRows = importResult.Rows.Select(r => new EditionRowViewModel(r)).ToList();

            // 4. Update stats
            CurrentFilePath = filePath;
            TotalRowCount = _allRows.Count;
            UnknownRowCount = unknownCount;
            ApprovedRowCount = 0;

            // 5. Apply filter and refresh grid
            ApplyFilter();

            // 6. Save session to DB
            var session = new EditionFileSession
            {
                filepath = filePath,
                sourcetype = SelectedSourceType.ToString(),
                totalrows = TotalRowCount,
                unknownrows = UnknownRowCount,
                addedat = DateTime.Now
            };
            await _editionFileService.SaveSessionAsync(session);

            StatusMessage = $"Import terminé : {TotalRowCount} lignes, {UnknownRowCount} inconnues.";
        }, "Importation en cours...");
    }

    /// <summary>
    /// Approve the selected row: add unknown values to library, insert Medic
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteRowAction))]
    private async Task ApproveRowAsync()
    {
        if (SelectedRow == null) return;

        var confirmed = await _dialogService.ShowConfirmAsync(
            "Approuver la ligne",
            $"Ajouter les données inconnues de '{SelectedRow.ItemName}' à la bibliothèque et insérer le médicament ?");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            await _editionFileService.ApproveRowAsync(SelectedRow.Row);
            ApprovedRowCount++;
            UnknownRowCount = _allRows.Count(r => r.HasUnknownFields);
            ApplyFilter();
            await _dialogService.ShowSuccessAsync("Approuvé", $"'{SelectedRow.ItemName}' ajouté avec succès.");
        }, "Approbation...");
    }

    /// <summary>
    /// Reject the selected row: clear unknown fields, set Désaffecté
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteRowAction))]
    private async Task RejectRowAsync()
    {
        if (SelectedRow == null) return;

        await ExecuteAsync(async () =>
        {
            await _editionFileService.RejectRowAsync(SelectedRow.Row);
            UnknownRowCount = _allRows.Count(r => r.HasUnknownFields);
            ApplyFilter();
        }, "Rejet...");
    }

    /// <summary>
    /// Mark selected row as new
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteRowAction))]
    private void MarkAsNew()
    {
        if (SelectedRow == null) return;
        SelectedRow.Row.ActionFlag = ActionFlag.Nouveau;
    }

    /// <summary>
    /// Mark selected row for deletion
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteRowAction))]
    private void MarkForDeletion()
    {
        if (SelectedRow == null) return;
        SelectedRow.Row.ActionFlag = ActionFlag.MarquerSuppression;
    }

    /// <summary>
    /// Reset selected row to original imported state
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteRowAction))]
    private void ResetRow()
    {
        if (SelectedRow == null) return;
        SelectedRow.Row.ActionFlag = ActionFlag.Reinitialiser;
    }

    /// <summary>
    /// Open the library manager dialog (D-05: dialog, not separate page)
    /// </summary>
    [RelayCommand]
    private async Task OpenLibraryManagerAsync()
    {
        await _dialogService.ShowInfoAsync(
            "Gestion de la bibliothèque",
            "Le gestionnaire de bibliothèque sera disponible ici.");
    }

    /// <summary>
    /// Similarity search for a selected row
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteRowAction))]
    private async Task SimilaritySearchAsync()
    {
        if (SelectedRow == null) return;
        await _dialogService.ShowInfoAsync(
            "Recherche de médicament similaire",
            $"Recherche pour : {SelectedRow.ItemName}");
    }

    /// <summary>
    /// Export current filtered rows to Excel
    /// </summary>
    [RelayCommand]
    private async Task ExportAsync()
    {
        var filePath = _dialogService.ShowSaveFileDialog(
            "Fichiers Excel|*.xlsx",
            $"Fichier_edition_{DateTime.Now:yyyyMMdd}",
            "Exporter le fichier d'édition");

        if (string.IsNullOrEmpty(filePath))
            return;

        await ExecuteAsync(async () =>
        {
            var rowsToExport = Rows.Select(vm => vm.Row).ToList();
            await _editionFileService.ExportEditionFileAsync(rowsToExport, filePath);
            await _dialogService.ShowSuccessAsync("Export réussi", $"Fichier exporté vers {filePath}");
        }, "Export en cours...");
    }

    // ─── Can-execute helpers ──────────────────────────────────────────────

    private bool CanExecuteRowAction() => SelectedRow != null;

    // ─── Filter logic ─────────────────────────────────────────────────────

    public void ApplyFilter()
    {
        var filtered = FilterType switch
        {
            "Actifs"              => _allRows.Where(r => r.RowStatus == RowStatus.Active),
            "Inactifs"            => _allRows.Where(r => r.RowStatus == RowStatus.Inactive),
            "Affectés"            => _allRows.Where(r => r.ActionFlag == ActionFlag.Affecte),
            "Non-affectés"        => _allRows.Where(r => r.ActionFlag == ActionFlag.AjouterNew || r.ActionFlag == ActionFlag.None),
            "Méd.[V.E.I]"        => _allRows.Where(r => !string.IsNullOrWhiteSpace(r.Veic)),
            "Avec A.P"            => _allRows.Where(r => r.IsAp != 0),
            "Sans A.P"            => _allRows.Where(r => r.IsAp == 0),
            "Remboursés"          => _allRows.Where(r => r.IsRemboursable != 0),
            "Non-remboursés"      => _allRows.Where(r => r.IsRemboursable == 0),
            "Sélectionnés"        => _allRows.Where(r => r.IsSelected),
            "Non-sélectionnés"    => _allRows.Where(r => !r.IsSelected),
            "Changements de prix" => _allRows.Where(r => r.HasPriceChanged),
            _                     => _allRows // "Tous"
        };

        Rows = new ObservableCollection<EditionRowViewModel>(filtered);
    }
}
