using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.ViewModels;

/// <summary>
/// ViewModel for the similarity search dialog.
/// Displays medics from DB that are similar to a given EditionRow.
/// </summary>
public partial class SimilaritySearchViewModel : ObservableObject
{
    private readonly IEditionFileService _editionFileService;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<SimilarMedicResult> _results = new();

    [ObservableProperty]
    private SimilarMedicResult? _selectedResult;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public event Action<bool>? RequestClose;

    public SimilaritySearchViewModel(IEditionFileService editionFileService)
    {
        _editionFileService = editionFileService;
    }

    /// <summary>
    /// Initialize the dialog and run the search for the given row.
    /// </summary>
    public async Task InitializeAsync(EditionRow row)
    {
        SearchQuery = row.ItemName;
        IsLoading = true;
        StatusMessage = "Recherche en cours...";

        try
        {
            var results = await _editionFileService.SearchSimilarAsync(row);
            Results = new ObservableCollection<SimilarMedicResult>(results);
            StatusMessage = $"{results.Count} résultat(s) trouvé(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erreur : {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke(SelectedResult != null);
    }
}
