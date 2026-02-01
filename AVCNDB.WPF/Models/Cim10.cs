using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVCNDB.WPF.Models;

/// <summary>
/// Classification Internationale des Maladies (CIM-10)
/// </summary>
[Table("cim10")]
public class Cim10 : ITrackable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int recordid { get; set; }
    
    [Required]
    [StringLength(10)]
    public string code { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string itemname { get; set; } = string.Empty;
    
    [StringLength(10)]
    public string parentcode { get; set; } = string.Empty;
    
    public int level { get; set; }
    
    [StringLength(20)]
    public string category { get; set; } = string.Empty;
    
    public DateTime? addedat { get; set; }
    public DateTime? updatedat { get; set; }
}
