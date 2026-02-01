namespace AVCNDB.WPF.Contracts.Services;

/// <summary>
/// Interface du service de dialogue
/// Gère l'affichage des boîtes de dialogue
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Affiche un message d'information
    /// </summary>
    Task ShowInfoAsync(string title, string message);
    
    /// <summary>
    /// Affiche un message de succès
    /// </summary>
    Task ShowSuccessAsync(string title, string message);
    
    /// <summary>
    /// Affiche un message d'avertissement
    /// </summary>
    Task ShowWarningAsync(string title, string message);
    
    /// <summary>
    /// Affiche un message d'erreur
    /// </summary>
    Task ShowErrorAsync(string title, string message);
    
    /// <summary>
    /// Affiche une boîte de confirmation Oui/Non
    /// </summary>
    /// <returns>true si l'utilisateur confirme</returns>
    Task<bool> ShowConfirmAsync(string title, string message);
    
    /// <summary>
    /// Affiche une boîte de saisie de texte
    /// </summary>
    /// <returns>Le texte saisi ou null si annulé</returns>
    Task<string?> ShowInputAsync(string title, string message, string defaultValue = "");
    
    /// <summary>
    /// Affiche un sélecteur de fichier pour ouvrir
    /// </summary>
    /// <param name="filter">Filtre des fichiers (ex: "Excel Files|*.xlsx")</param>
    /// <returns>Chemin du fichier ou null</returns>
    string? ShowOpenFileDialog(string filter, string title = "Ouvrir un fichier");
    
    /// <summary>
    /// Affiche un sélecteur de fichier pour sauvegarder
    /// </summary>
    /// <param name="filter">Filtre des fichiers</param>
    /// <param name="defaultFileName">Nom de fichier par défaut</param>
    /// <returns>Chemin du fichier ou null</returns>
    string? ShowSaveFileDialog(string filter, string defaultFileName = "", string title = "Enregistrer sous");
}
