using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVCNDB.WPF.Models;

/// <summary>
/// Médecins associés
/// </summary>
[Table("associates")]
public class Associates : ITrackable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int recordid { get; set; }
    
    [Required]
    [StringLength(80)]
    public string itemname { get; set; } = string.Empty;
    
    [StringLength(60)]
    public string specialite { get; set; } = string.Empty;
    
    [StringLength(150)]
    public string address { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string localite { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string gouvern { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string phone { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string mobile { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string email { get; set; } = string.Empty;
    
    public int isactive { get; set; } = 1;
    
    public DateTime? addedat { get; set; }
    public DateTime? updatedat { get; set; }
}
