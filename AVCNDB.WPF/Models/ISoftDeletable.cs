namespace AVCNDB.WPF.Models;

/// <summary>
/// Interface de suppression logique (soft-delete).
/// Les entités implémentant cette interface ne sont pas supprimées physiquement
/// mais marquées via leur date de suppression.
/// </summary>
public interface ISoftDeletable
{
    DateTime? deletedat { get; set; }
}
