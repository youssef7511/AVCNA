using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVCNDB.WPF.Models;

/// <summary>
/// Types de contre-indications
/// </summary>
[Table("citypes")]
public class Citypes : ITrackable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int recordid { get; set; }
    
    [Required]
    [StringLength(50)]
    public string itemname { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string subvalue { get; set; } = string.Empty;
    
    public DateTime? addedat { get; set; }
    public DateTime? updatedat { get; set; }
}
