using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVCNDB.WPF.Models;

/// <summary>
/// Localités (villes/communes)
/// </summary>
[Table("localites")]
public class Localites : ITrackable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int recordid { get; set; }
    
    [Required]
    [StringLength(80)]
    public string itemname { get; set; } = string.Empty;
    
    public int gouvernid { get; set; }
    
    [StringLength(50)]
    public string gouvernname { get; set; } = string.Empty;
    
    [StringLength(10)]
    public string postalcode { get; set; } = string.Empty;
    
    public DateTime? addedat { get; set; }
    public DateTime? updatedat { get; set; }
}
