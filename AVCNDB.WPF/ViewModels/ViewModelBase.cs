using CommunityToolkit.Mvvm.ComponentModel;

namespace AVCNDB.WPF.ViewModels;

/// <summary>
/// Classe de base pour tous les ViewModels
/// Fournit les fonctionnalités communes MVVM
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Exécute une action de manière asynchrone avec gestion du loading
    /// </summary>
    protected async Task ExecuteAsync(Func<Task> action, string? loadingMessage = null)
    {
        try
        {
            IsBusy = true;
            IsLoading = true;
            ErrorMessage = null;
            StatusMessage = loadingMessage ?? "Chargement...";

            await action();

            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Erreur";
        }
        finally
        {
            IsBusy = false;
            IsLoading = false;
        }
    }

    /// <summary>
    /// Exécute une action avec retour de valeur de manière asynchrone
    /// </summary>
    protected async Task<T?> ExecuteAsync<T>(Func<Task<T>> action, string? loadingMessage = null)
    {
        try
        {
            IsBusy = true;
            IsLoading = true;
            ErrorMessage = null;
            StatusMessage = loadingMessage ?? "Chargement...";

            var result = await action();

            StatusMessage = string.Empty;
            return result;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Erreur";
            return default;
        }
        finally
        {
            IsBusy = false;
            IsLoading = false;
        }
    }

    /// <summary>
    /// Appelé lors de la navigation vers ce ViewModel
    /// </summary>
    public virtual void OnNavigatedTo(object? parameter) { }

    /// <summary>
    /// Appelé lors de la navigation depuis ce ViewModel
    /// </summary>
    public virtual void OnNavigatedFrom() { }
}
