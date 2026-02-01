using ClosedXML.Excel;

namespace AVCNDB.WPF.Contracts.Services;

/// <summary>
/// Interface du service Excel
/// Gère l'import/export de données Excel
/// </summary>
public interface IExcelService
{
    /// <summary>
    /// Importe des données depuis un fichier Excel
    /// </summary>
    /// <typeparam name="T">Type d'entité</typeparam>
    /// <param name="filePath">Chemin du fichier Excel</param>
    /// <param name="sheetName">Nom de la feuille (optionnel)</param>
    /// <returns>Liste des entités importées</returns>
    Task<IEnumerable<T>> ImportAsync<T>(string filePath, string? sheetName = null) where T : class, new();
    
    /// <summary>
    /// Exporte des données vers un fichier Excel
    /// </summary>
    /// <typeparam name="T">Type d'entité</typeparam>
    /// <param name="data">Données à exporter</param>
    /// <param name="filePath">Chemin du fichier de destination</param>
    /// <param name="sheetName">Nom de la feuille</param>
    Task ExportAsync<T>(IEnumerable<T> data, string filePath, string sheetName = "Data") where T : class;
    
    /// <summary>
    /// Exporte des données vers un stream (pour téléchargement)
    /// </summary>
    Task<byte[]> ExportToBytesAsync<T>(IEnumerable<T> data, string sheetName = "Data") where T : class;
    
    /// <summary>
    /// Valide la structure d'un fichier Excel avant import
    /// </summary>
    /// <param name="filePath">Chemin du fichier</param>
    /// <param name="expectedColumns">Colonnes attendues</param>
    /// <returns>Résultat de validation avec erreurs éventuelles</returns>
    Task<ExcelValidationResult> ValidateFileAsync(string filePath, IEnumerable<string> expectedColumns);
}

/// <summary>
/// Résultat de validation d'un fichier Excel
/// </summary>
public class ExcelValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public int RowCount { get; set; }
    public List<string> FoundColumns { get; set; } = new();
    public List<string> MissingColumns { get; set; } = new();
}
