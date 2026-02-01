namespace AVCNDB.WPF.Models;

/// <summary>
/// Interface de traçabilité implémentée par toutes les entités
/// Permet l'audit automatique des dates de création et modification
/// </summary>
public interface ITrackable
{
    /// <summary>
    /// Identifiant unique (clé primaire auto-incrémentée)
    /// </summary>
    int recordid { get; set; }

    /// <summary>
    /// Date de création de l'enregistrement
    /// </summary>
    DateTime? addedat { get; set; }

    /// <summary>
    /// Date de dernière modification
    /// </summary>
    DateTime? updatedat { get; set; }
}
