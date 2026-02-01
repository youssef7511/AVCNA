using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVCNDB.WPF.Models;

/// <summary>
/// Voies d'administration (orale, injectable, etc.)
/// </summary>
[Table("voie")]
public class Voies : ITrackable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int recordid { get; set; }

    [Required]
    [StringLength(40)]
    public string itemname { get; set; } = string.Empty;

    [StringLength(30)]
    public string abname { get; set; } = string.Empty;

    [StringLength(50)]
    public string subvalue { get; set; } = string.Empty;

    public DateTime? addedat { get; set; }
    public DateTime? updatedat { get; set; }
}
