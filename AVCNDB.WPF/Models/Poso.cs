using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVCNDB.WPF.Models;

/// <summary>
/// Posologies (schémas de dosage)
/// </summary>
[Table("poso")]
public class Poso : ITrackable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int recordid { get; set; }

    /// <summary>UI-only: sélection par checkbox</summary>
    [NotMapped]
    public bool IsChecked { get; set; }

    [Required]
    [StringLength(100)]
    public string itemname { get; set; } = string.Empty;

    [Column(TypeName = "decimal(5, 2)")]
    public decimal qty { get; set; }

    /// <summary>
    /// Forme galénique utilisée par cette posologie (ex. « Comprimé », « Ampoule »).
    /// Clé étrangère vers <see cref="Formes.itemname"/>, NULL si non renseignée.
    /// La contrainte est posée au niveau MariaDB : ON UPDATE CASCADE, ON DELETE SET NULL.
    /// </summary>
    [StringLength(50)]
    public string? posoform { get; set; }

    /// <summary>
    /// Verbe générique associé à la forme (ex. « avaler » pour Capsule,
    /// « application » pour Pommade). Snapshot copié depuis
    /// <see cref="Formes.posoform"/> au moment où l'admin sélectionne la Forme
    /// dans le dialogue Posologie. Ce snapshot fige la valeur dans l'historique
    /// de la posologie : modifier le verbe sur la Forme plus tard n'affecte pas
    /// les Posologies déjà enregistrées (à recalculer explicitement via le
    /// bouton « Rafraîchir le verbe » si souhaité).
    /// </summary>
    [StringLength(50)]
    public string? posoverb { get; set; }

    [Column(TypeName = "decimal(7, 2)")]
    public decimal prises { get; set; }

    [StringLength(50)]
    public string periode { get; set; } = string.Empty;

    [StringLength(200)]
    public string conditions { get; set; } = string.Empty;

    [StringLength(100)]
    public string nameformul { get; set; } = string.Empty;

    [StringLength(100)]
    public string subvalue { get; set; } = string.Empty;

    public DateTime? addedat { get; set; }
    public DateTime? updatedat { get; set; }
}
