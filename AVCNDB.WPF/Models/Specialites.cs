using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVCNDB.WPF.Models;

/// <summary>
/// Spécialités médicales
/// </summary>
[Table("specialites")]
public class Specialites : ITrackable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int recordid { get; set; }
    
    [Required]
    [StringLength(80)]
    public string itemname { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string abname { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string subvalue { get; set; } = string.Empty;
    
    public DateTime? addedat { get; set; }
    public DateTime? updatedat { get; set; }
}
