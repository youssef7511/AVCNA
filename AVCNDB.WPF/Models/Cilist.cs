using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVCNDB.WPF.Models;

/// <summary>
/// Liste des contre-indications par médicament
/// </summary>
[Table("cilist")]
public class Cilist : ITrackable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int recordid { get; set; }
    
    public int medicid { get; set; }
    public int ciid { get; set; }
    
    [StringLength(150)]
    public string medicname { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string ciname { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string level { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string description { get; set; } = string.Empty;
    
    public DateTime? addedat { get; set; }
    public DateTime? updatedat { get; set; }
}
