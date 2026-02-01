using AVCNDB.WPF.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AVCNDB.WPF.Services;

/// <summary>
/// Service de navigation entre les pages
/// </summary>
public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Stack<(Type viewModelType, object? parameter)> _navigationStack = new();
    
    private object? _currentView;
    
    public object? CurrentView
    {
        get => _currentView;
        private set
        {
            _currentView = value;
            NavigationChanged?.Invoke();
        }
    }
    
    public event Action? NavigationChanged;
    
    public bool CanGoBack => _navigationStack.Count > 1;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void NavigateTo<T>(object? parameter = null) where T : class
    {
        var viewModel = _serviceProvider.GetRequiredService<T>();
        
        // Sauvegarde dans l'historique
        _navigationStack.Push((typeof(T), parameter));
        
        // Initialiser le ViewModel si nécessaire
        if (viewModel is INavigationAware navigationAware)
        {
            navigationAware.OnNavigatedTo(parameter);
        }
        
        CurrentView = viewModel;
    }

    public void NavigateTo(string pageKey, object? parameter = null)
    {
        // Résolution dynamique par nom de page
        var viewModelType = GetViewModelType(pageKey);
        if (viewModelType != null)
        {
            var viewModel = _serviceProvider.GetRequiredService(viewModelType);
            
            _navigationStack.Push((viewModelType, parameter));
            
            if (viewModel is INavigationAware navigationAware)
            {
                navigationAware.OnNavigatedTo(parameter);
            }
            
            CurrentView = viewModel;
        }
    }

    public bool GoBack()
    {
        if (!CanGoBack) return false;
        
        // Retirer la page actuelle
        _navigationStack.Pop();
        
        // Récupérer la page précédente
        var (previousType, parameter) = _navigationStack.Peek();
        var viewModel = _serviceProvider.GetRequiredService(previousType);
        
        if (viewModel is INavigationAware navigationAware)
        {
            navigationAware.OnNavigatedTo(parameter);
        }
        
        CurrentView = viewModel;
        return true;
    }

    private Type? GetViewModelType(string pageKey)
    {
        // Mapper les clés de page vers les types de ViewModel
        var assembly = typeof(NavigationService).Assembly;
        var typeName = $"AVCNDB.WPF.ViewModels.{pageKey}ViewModel";
        return assembly.GetType(typeName);
    }
}

/// <summary>
/// Interface pour les ViewModels qui veulent recevoir des notifications de navigation
/// </summary>
public interface INavigationAware
{
    void OnNavigatedTo(object? parameter);
    void OnNavigatedFrom();
}
