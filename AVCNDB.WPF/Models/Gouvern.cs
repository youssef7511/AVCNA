using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVCNDB.WPF.Models;

/// <summary>
/// Gouvernorats (régions administratives)
/// </summary>
[Table("gouvern")]
public class Gouvern : ITrackable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int recordid { get; set; }
    
    [Required]
    [StringLength(50)]
    public string itemname { get; set; } = string.Empty;
    
    [StringLength(10)]
    public string code { get; set; } = string.Empty;
    
    public DateTime? addedat { get; set; }
    public DateTime? updatedat { get; set; }
}
