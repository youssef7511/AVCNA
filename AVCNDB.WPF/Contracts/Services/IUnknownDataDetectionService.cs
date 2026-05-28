namespace AVCNDB.WPF.Contracts.Services;

/// <summary>
/// Résultat de détection pour un champ unique
/// </summary>
public class FieldDetectionResult
{
    /// <summary>Nom du champ (ex: "Dci", "Labo", "Forme")</summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>Valeur importée depuis le fichier</summary>
    public string ImportedValue { get; set; } = string.Empty;

    /// <summary>True si la valeur est reconnue dans la bibliothèque</summary>
    public bool IsKnown { get; set; }

    /// <summary>Score de similarité (0-100) — via FuzzySharp</summary>
    public int Score { get; set; }

    /// <summary>Meilleure correspondance trouvée dans la bibliothèque</summary>
    public string? BestMatch { get; set; }
}

/// <summary>
/// Rapport de détection pour une ligne complète (tous les champs vérifiés)
/// </summary>
public class DetectionReport
{
    /// <summary>Index de la ligne dans le fichier d'édition</summary>
    public int RowIndex { get; set; }

    /// <summary>Liste des résultats par champ</summary>
    public List<FieldDetectionResult> FieldResults { get; set; } = new();

    /// <summary>True si tous les champs sont connus</summary>
    public bool AllFieldsKnown => FieldResults.All(f => f.IsKnown);

    /// <summary>Noms des champs inconnus</summary>
    public List<string> UnknownFieldNames => FieldResults
        .Where(f => !f.IsKnown)
        .Select(f => f.FieldName)
        .ToList();
}

/// <summary>
/// Service de détection de données inconnues par comparaison floue (ML).
/// Compare les valeurs importées contre les tables de bibliothèque (DCI, Labos, Familles, etc.)
/// </summary>
public interface IUnknownDataDetectionService
{
    /// <summary>
    /// Vérifie une valeur unique contre une liste de références
    /// </summary>
    /// <param name="fieldName">Nom du champ</param>
    /// <param name="value">Valeur à vérifier</param>
    /// <param name="knownValues">Valeurs connues de la bibliothèque</param>
    /// <returns>Résultat de détection</returns>
    FieldDetectionResult CheckValue(string fieldName, string value, IReadOnlyList<string> knownValues);

    /// <summary>
    /// Détecte les champs inconnus d'une ligne complète
    /// </summary>
    /// <param name="row">Ligne du fichier d'édition</param>
    /// <returns>Rapport de détection</returns>
    Task<DetectionReport> DetectAsync(Models.EditionRow row);

    /// <summary>
    /// Détecte les champs inconnus pour un lot de lignes (charge la bibliothèque une seule fois)
    /// </summary>
    /// <param name="rows">Lignes du fichier d'édition</param>
    /// <returns>Liste de rapports de détection</returns>
    Task<List<DetectionReport>> DetectBatchAsync(IReadOnlyList<Models.EditionRow> rows);

    /// <summary>
    /// Re-vérifie la valeur d'un champ de référence unique contre la bibliothèque,
    /// en réutilisant un instantané mis en cache. Sert à la re-validation lorsqu'une
    /// cellule est éditée manuellement (sélection dans un combo). Une valeur vide est
    /// considérée comme connue (pas d'alerte) ; un champ non soumis à détection l'est aussi.
    /// </summary>
    /// <param name="fieldName">Nom du champ de détection (ex: « Dci », « Forme », « Labo »).</param>
    /// <param name="value">Nouvelle valeur saisie.</param>
    /// <returns>True si la valeur est désormais reconnue dans la bibliothèque.</returns>
    Task<bool> IsFieldKnownAsync(string fieldName, string? value);

    /// <summary>
    /// Invalide l'instantané de bibliothèque mis en cache. À appeler après l'ajout de
    /// nouvelles valeurs (p.ex. approbation d'une ligne) pour que la prochaine
    /// re-validation recharge des données fraîches.
    /// </summary>
    void InvalidateLibraryCache();
}
