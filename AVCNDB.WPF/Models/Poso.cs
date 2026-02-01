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

    [Required]
    [StringLength(100)]
    public string itemname { get; set; } = string.Empty;

    [Column(TypeName = "decimal(5, 2)")]
    public decimal qty { get; set; }

    [StringLength(30)]
    public string posoform { get; set; } = string.Empty;

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
