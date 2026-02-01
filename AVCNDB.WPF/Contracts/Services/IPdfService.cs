namespace AVCNDB.WPF.Contracts.Services;

/// <summary>
/// Interface du service de génération de rapports PDF
/// </summary>
public interface IPdfService
{
    /// <summary>
    /// Génère un rapport PDF pour un médicament
    /// </summary>
    /// <param name="medicId">ID du médicament</param>
    /// <param name="outputPath">Chemin du fichier de sortie</param>
    Task GenerateMedicReportAsync(int medicId, string outputPath);
    
    /// <summary>
    /// Génère un rapport PDF pour une liste de médicaments
    /// </summary>
    /// <param name="medicIds">IDs des médicaments</param>
    /// <param name="outputPath">Chemin du fichier de sortie</param>
    Task GenerateMedicListReportAsync(IEnumerable<int> medicIds, string outputPath);
    
    /// <summary>
    /// Génère un rapport de stock
    /// </summary>
    /// <param name="outputPath">Chemin du fichier de sortie</param>
    /// <param name="includeAlerts">Inclure les alertes de stock</param>
    Task GenerateStockReportAsync(string outputPath, bool includeAlerts = true);
    
    /// <summary>
    /// Génère un rapport d'interactions médicamenteuses
    /// </summary>
    /// <param name="dciNames">Noms des DCI à analyser</param>
    /// <param name="outputPath">Chemin du fichier de sortie</param>
    Task GenerateInteractionReportAsync(IEnumerable<string> dciNames, string outputPath);
    
    /// <summary>
    /// Génère un rapport personnalisé
    /// </summary>
    /// <typeparam name="T">Type de données</typeparam>
    /// <param name="data">Données à inclure</param>
    /// <param name="title">Titre du rapport</param>
    /// <param name="outputPath">Chemin du fichier de sortie</param>
    Task GenerateCustomReportAsync<T>(IEnumerable<T> data, string title, string outputPath) where T : class;
    
    /// <summary>
    /// Génère un rapport en mémoire (bytes)
    /// </summary>
    Task<byte[]> GenerateMedicReportToBytesAsync(int medicId);
}
