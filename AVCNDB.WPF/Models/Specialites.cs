using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVCNDB.WPF.Models;

/// <summary>
/// Spécialités médicales
/// </summary>
[Table("specialites")]
public class Specialites : ITrackable, ISoftDeletable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int recordid { get; set; }

    /// <summary>UI-only: sélection par checkbox</summary>
    [NotMapped]
    public bool IsChecked { get; set; }
    
    [Required]
    [StringLength(80)]
    public string itemname { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string abname { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string subvalue { get; set; } = string.Empty;
    
    public DateTime? addedat { get; set; }
    public DateTime? updatedat { get; set; }
    public DateTime? deletedat { get; set; }
}
