namespace AVCNDB.WPF.Contracts.Services;

/// <summary>
/// Interface du service de thème
/// Gère le changement de thème (clair/sombre)
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Thème actuellement utilisé
    /// </summary>
    AppTheme CurrentTheme { get; }
    
    /// <summary>
    /// Événement déclenché lors du changement de thème
    /// </summary>
    event Action<AppTheme>? ThemeChanged;
    
    /// <summary>
    /// Applique un thème spécifique
    /// </summary>
    void SetTheme(AppTheme theme);
    
    /// <summary>
    /// Bascule entre les thèmes clair et sombre
    /// </summary>
    void ToggleTheme();
    
    /// <summary>
    /// Initialise le thème depuis les paramètres
    /// </summary>
    void Initialize();
}

/// <summary>
/// Énumération des thèmes disponibles
/// </summary>
public enum AppTheme
{
    Light,
    Dark,
    System
}
