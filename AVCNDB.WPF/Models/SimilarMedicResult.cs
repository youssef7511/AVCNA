namespace AVCNDB.WPF.Models;

/// <summary>
/// Résultat d'une recherche de médicament similaire (FuzzySharp TokenSortRatio).
/// </summary>
public class SimilarMedicResult
{
    public int RecordId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string Dci { get; set; } = string.Empty;
    public string Forme { get; set; } = string.Empty;
    public string Labo { get; set; } = string.Empty;
    public string Voie { get; set; } = string.Empty;
    public int Price { get; set; }
    public int Score { get; set; }
}
