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
    /// <summary>True si les données étaient en orientation verticale (transposées)</summary>
    public bool WasTransposed { get; set; }
    /// <summary>Avertissements sur les en-têtes de colonnes (typos corrigés, colonnes ignorées)</summary>
    public List<HeaderWarning> HeaderWarnings { get; set; } = new();
}

/// <summary>
/// Avertissement sur un en-tête de colonne lors de l'import
/// </summary>
public class HeaderWarning
{
    public enum WarningType { FuzzyCorrected, Unrecognized }
    public WarningType Type { get; set; }
    /// <summary>Nom original de l'en-tête dans le fichier Excel</summary>
    public string OriginalHeader { get; set; } = string.Empty;
    /// <summary>Nom corrigé (si fuzzy-matched), null si non reconnu</summary>
    public string? CorrectedTo { get; set; }
    /// <summary>Champ EditionRow mappé</summary>
    public string? MappedField { get; set; }
    /// <summary>Score de similarité FuzzySharp (0-100)</summary>
    public int FuzzyScore { get; set; }
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

    /// <summary>
    /// Recherche les médicaments similaires en base via FuzzySharp (par nom + DCI).
    /// </summary>
    /// <param name="row">Ligne source pour la recherche</param>
    /// <param name="maxResults">Nombre maximum de résultats (défaut 20)</param>
    /// <returns>Liste de résultats triés par score décroissant</returns>
    Task<List<SimilarMedicResult>> SearchSimilarAsync(EditionRow row, int maxResults = 20);
}
