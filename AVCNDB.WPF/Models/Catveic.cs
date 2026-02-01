using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVCNDB.WPF.Models;

/// <summary>
/// Catégories VEIC (Vigilance des Effets Indésirables des Conduites)
/// </summary>
[Table("catveic")]
public class Catveic : ITrackable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int recordid { get; set; }
    
    [Required]
    [StringLength(50)]
    public string itemname { get; set; } = string.Empty;
    
    [StringLength(10)]
    public string code { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string description { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string pictogram { get; set; } = string.Empty;
    
    public DateTime? addedat { get; set; }
    public DateTime? updatedat { get; set; }
}
