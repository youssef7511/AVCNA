using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVCNDB.WPF.Models;

/// <summary>
/// Familles thérapeutiques de médicaments
/// </summary>
[Table("family")]
public class Families : ITrackable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int recordid { get; set; }

    [Required]
    [StringLength(80)]
    public string itemname { get; set; } = string.Empty;

    [StringLength(100)]
    public string subvalue { get; set; } = string.Empty;

    public DateTime? addedat { get; set; }
    public DateTime? updatedat { get; set; }
}
