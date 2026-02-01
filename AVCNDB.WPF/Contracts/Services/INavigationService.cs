namespace AVCNDB.WPF.Contracts.Services;

/// <summary>
/// Interface du service de navigation
/// Gère la navigation entre les pages de l'application
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Page actuellement affichée
    /// </summary>
    object? CurrentView { get; }
    
    /// <summary>
    /// Événement déclenché lors d'un changement de page
    /// </summary>
    event Action? NavigationChanged;
    
    /// <summary>
    /// Navigue vers une page par son nom de ViewModel
    /// </summary>
    /// <typeparam name="T">Type du ViewModel</typeparam>
    /// <param name="parameter">Paramètre optionnel de navigation</param>
    void NavigateTo<T>(object? parameter = null) where T : class;
    
    /// <summary>
    /// Navigue vers une page par son nom
    /// </summary>
    /// <param name="pageKey">Clé de la page</param>
    /// <param name="parameter">Paramètre optionnel</param>
    void NavigateTo(string pageKey, object? parameter = null);
    
    /// <summary>
    /// Retourne à la page précédente
    /// </summary>
    bool GoBack();
    
    /// <summary>
    /// Indique si on peut revenir en arrière
    /// </summary>
    bool CanGoBack { get; }
}
