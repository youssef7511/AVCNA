using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVCNDB.WPF.Models;

/// <summary>
/// Formes pharmaceutiques (comprimé, gélule, sirop, etc.)
/// </summary>
[Table("formes")]
public class Formes : ITrackable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int recordid { get; set; }

    [Required]
    [StringLength(50)]
    public string itemname { get; set; } = string.Empty;

    [StringLength(50)]
    public string subvalue { get; set; } = string.Empty;

    [StringLength(25)]
    public string formgroup { get; set; } = string.Empty;

    [StringLength(230)]
    public string abname { get; set; } = string.Empty;

    [StringLength(30)]
    public string posoform { get; set; } = string.Empty;

    [StringLength(30)]
    public string posoname { get; set; } = string.Empty;

    public DateTime? addedat { get; set; }
    public DateTime? updatedat { get; set; }
}
