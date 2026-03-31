using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using AVCNDB.WPF.Services;

namespace AVCNDB.WPF.ViewModels;

/// <summary>
/// Classe de base pour tous les ViewModels
/// Fournit les fonctionnalités communes MVVM
/// </summary>
public abstract partial class ViewModelBase : ObservableObject, INavigationAware, IDisposable
{
    /// <summary>
    /// Serialises all DB / async operations so that a second call
    /// waits for the first one to finish instead of hitting the
    /// same DbContext concurrently.
    /// </summary>
    private readonly SemaphoreSlim _busy = new(1, 1);
    private readonly AsyncLocal<int> _executionDepth = new();

    /// <summary>
    /// CancellationTokenSource used for search debouncing.
    /// Each new keystroke cancels the previous pending search.
    /// </summary>
    private CancellationTokenSource? _debounceCts;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Exécute une action de manière asynchrone avec gestion du loading.
    /// Un SemaphoreSlim garantit qu'une seule opération tourne à la fois
    /// (évite "A second operation was started on this context instance…").
    /// </summary>
    protected async Task ExecuteAsync(Func<Task> action, string? loadingMessage = null)
    {
        // Re-entrant call on the same async flow: do not wait on the same lock.
        if (_executionDepth.Value > 0)
        {
            _executionDepth.Value++;
            try
            {
                await action();
            }
            finally
            {
                _executionDepth.Value--;
            }
            return;
        }

        await _busy.WaitAsync();
        _executionDepth.Value++;
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
            _executionDepth.Value--;
            IsBusy = false;
            IsLoading = false;
            _busy.Release();
        }
    }

    /// <summary>
    /// Exécute une action avec retour de valeur de manière asynchrone
    /// </summary>
    protected async Task<T?> ExecuteAsync<T>(Func<Task<T>> action, string? loadingMessage = null)
    {
        // Re-entrant call on the same async flow: do not wait on the same lock.
        if (_executionDepth.Value > 0)
        {
            _executionDepth.Value++;
            try
            {
                return await action();
            }
            finally
            {
                _executionDepth.Value--;
            }
        }

        await _busy.WaitAsync();
        _executionDepth.Value++;
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
            _executionDepth.Value--;
            IsBusy = false;
            IsLoading = false;
            _busy.Release();
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

    /// <summary>
    /// Debounce a search action: cancels any pending search and
    /// waits <paramref name="delayMs"/> ms before executing <paramref name="action"/>.
    /// Typical usage: call from OnSearchTextChanged partial method.
    /// </summary>
    protected void DebounceSearch(Func<Task> action, int delayMs = 300)
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delayMs, token);
                if (!token.IsCancellationRequested)
                {
                    await App.Current.Dispatcher.InvokeAsync(async () => await action());
                }
            }
            catch (TaskCanceledException) { /* expected when user types fast */ }
        }, token);
    }

    /// <summary>
    /// Dispose managed resources (SemaphoreSlim, CancellationTokenSource).
    /// </summary>
    public virtual void Dispose()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _busy.Dispose();
        GC.SuppressFinalize(this);
    }
}
