using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVCNDB.WPF.Models;

/// <summary>
/// Session d'import d'un fichier d'édition — persistée en base de données.
/// Trace chaque import: source, nombre de lignes, statut, etc.
/// </summary>
[Table("edition_file_sessions")]
public class EditionFileSession : ITrackable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int recordid { get; set; }

    /// <summary>
    /// Chemin du fichier source importé
    /// </summary>
    [Required]
    [StringLength(500)]
    public string filepath { get; set; } = string.Empty;

    /// <summary>
    /// Type de source (ex: "ExcelCNAM", "ExcelPCT", "ExcelSimple", "DBF")
    /// </summary>
    [Required]
    [StringLength(50)]
    public string sourcetype { get; set; } = string.Empty;

    /// <summary>
    /// Description / libellé du fichier d'édition
    /// </summary>
    [StringLength(200)]
    public string description { get; set; } = string.Empty;

    /// <summary>
    /// Nombre total de lignes importées
    /// </summary>
    public int totalrows { get; set; }

    /// <summary>
    /// Nombre de lignes avec des données inconnues détectées
    /// </summary>
    public int unknownrows { get; set; }

    /// <summary>
    /// Nombre de lignes approuvées par l'utilisateur
    /// </summary>
    public int approvedrows { get; set; }

    /// <summary>
    /// Nombre de lignes rejetées par l'utilisateur
    /// </summary>
    public int rejectedrows { get; set; }

    /// <summary>
    /// Statut de la session: "InProgress", "Completed", "Cancelled"
    /// </summary>
    [Required]
    [StringLength(20)]
    public string status { get; set; } = "InProgress";

    /// <summary>
    /// Opérateur ayant effectué l'import
    /// </summary>
    [StringLength(100)]
    public string operatedby { get; set; } = string.Empty;

    /// <summary>
    /// Date de fin de traitement
    /// </summary>
    public DateTime? completedat { get; set; }

    // ============================================
    // ITrackable
    // ============================================
    public DateTime? addedat { get; set; }
    public DateTime? updatedat { get; set; }
}
