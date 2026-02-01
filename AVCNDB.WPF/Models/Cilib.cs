using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVCNDB.WPF.Models;

/// <summary>
/// Contre-indications (bibliothèque)
/// </summary>
[Table("cilib")]
public class Cilib : ITrackable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int recordid { get; set; }
    
    [Required]
    [StringLength(100)]
    public string itemname { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string description { get; set; } = string.Empty;
    
    public int typeid { get; set; }
    
    [StringLength(50)]
    public string typename { get; set; } = string.Empty;
    
    public DateTime? addedat { get; set; }
    public DateTime? updatedat { get; set; }
}
