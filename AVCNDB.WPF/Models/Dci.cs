using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVCNDB.WPF.Models;

/// <summary>
/// DCI - Dénomination Commune Internationale (Substance Active)
/// </summary>
[Table("dci")]
public class Dci : ITrackable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int recordid { get; set; }

    /// <summary>UI-only: sélection par checkbox pour l'export</summary>
    [NotMapped]
    public bool IsChecked { get; set; }

    [Required]
    [StringLength(100)]
    public string itemname { get; set; } = string.Empty;

    [StringLength(100)]
    public string subvalue { get; set; } = string.Empty;

    [StringLength(500)]
    public string iteminfo { get; set; } = string.Empty;

    public DateTime? addedat { get; set; }
    public DateTime? updatedat { get; set; }
}
