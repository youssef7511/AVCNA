using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVCNDB.WPF.Models;

/// <summary>
/// Présentations (conditionnement: boîte de 20, flacon de 100ml, etc.)
/// </summary>
[Table("presents")]
public class Presents : ITrackable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int recordid { get; set; }

    [Required]
    [StringLength(80)]
    public string itemname { get; set; } = string.Empty;

    [StringLength(30)]
    public string abname { get; set; } = string.Empty;

    [StringLength(50)]
    public string subvalue { get; set; } = string.Empty;

    public DateTime? addedat { get; set; }
    public DateTime? updatedat { get; set; }
}
