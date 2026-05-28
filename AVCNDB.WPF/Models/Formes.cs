using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVCNDB.WPF.Models;

/// <summary>
/// Formes pharmaceutiques (comprimé, gélule, sirop, etc.)
/// </summary>
[Table("formes")]
public class Formes : ITrackable, ISoftDeletable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int recordid { get; set; }

    /// <summary>UI-only: sélection par checkbox</summary>
    [NotMapped]
    public bool IsChecked { get; set; }

    [Required]
    [StringLength(50)]
    public string itemname { get; set; } = string.Empty;

    [StringLength(50)]
    public string subvalue { get; set; } = string.Empty;

    [StringLength(25)]
    public string formgroup { get; set; } = string.Empty;

    [StringLength(230)]
    public string abname { get; set; } = string.Empty;

    /// <summary>
    /// Abréviation libre servant à l'auto-génération de la dénomination
    /// de posologie côté dialogue Médicament (ex. « appl. » pour Pommade).
    /// Champ purement applicatif, ne participe pas à la FK Poso → Forme.
    /// </summary>
    [StringLength(50)]
    public string? posoform { get; set; }

    public DateTime? addedat { get; set; }
    public DateTime? updatedat { get; set; }
    public DateTime? deletedat { get; set; }
}
