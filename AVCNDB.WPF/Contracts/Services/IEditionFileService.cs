using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.Contracts.Services;

/// <summary>
/// Type de source pour l'import du fichier d'édition
/// </summary>
public enum EditionSourceType
{
    ExcelCNAM,
    ExcelPCT,
    ExcelPCTComplete,
    ExcelSimple,
    CatalogueLabo,
    ListePharmacie
}

/// <summary>
/// Résultat d'un import de fichier d'édition
/// </summary>
public class ImportResult
{
    public bool Success { get; set; }
    public List<EditionRow> Rows { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public int TotalRowsRead { get; set; }
    public int SkippedRows { get; set; }
}

/// <summary>
/// Service de gestion du fichier d'édition.
/// Orchestre l'import Excel, la validation ML, l'approbation/rejet et l'export.
/// </summary>
public interface IEditionFileService
{
    /// <summary>
    /// Importe un fichier Excel et le parse en liste de EditionRow
    /// </summary>
    /// <param name="filePath">Chemin du fichier Excel (.xls, .xlsx)</param>
    /// <param name="sourceType">Type de source sélectionné</param>
    /// <returns>Résultat d'import avec les lignes parsées</returns>
    Task<ImportResult> ImportExcelAsync(string filePath, EditionSourceType sourceType);

    /// <summary>
    /// Lance la détection ML sur toutes les lignes: compare chaque champ 
    /// contre la bibliothèque et marque les champs inconnus
    /// </summary>
    /// <param name="rows">Lignes importées</param>
    /// <returns>Nombre de lignes contenant des champs inconnus</returns>
    Task<int> ValidateAgainstLibraryAsync(List<EditionRow> rows);

    /// <summary>
    /// Approuve une ligne: ajoute les valeurs inconnues aux tables de bibliothèque
    /// et insère/met à jour le medic en base
    /// </summary>
    /// <param name="row">Ligne à approuver</param>
    Task ApproveRowAsync(EditionRow row);

    /// <summary>
    /// Rejette une ligne: marque comme désaffectée et efface les champs inconnus
    /// </summary>
    /// <param name="row">Ligne à rejeter</param>
    Task RejectRowAsync(EditionRow row);

    /// <summary>
    /// Exporte les lignes de l'édition vers un fichier Excel
    /// </summary>
    /// <param name="rows">Lignes à exporter</param>
    /// <param name="filePath">Chemin de destination</param>
    Task ExportEditionFileAsync(List<EditionRow> rows, string filePath);

    /// <summary>
    /// Sauvegarde la session d'import en base de données
    /// </summary>
    /// <param name="session">Session à persister</param>
    Task SaveSessionAsync(EditionFileSession session);
}
