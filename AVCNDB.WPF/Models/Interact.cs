using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVCNDB.WPF.Models;

/// <summary>
/// Interactions médicamenteuses
/// </summary>
[Table("interact")]
public class Interact : ITrackable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int recordid { get; set; }
    
    [StringLength(100)]
    public string dci1 { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string dci2 { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string level { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string description { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string conduite { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string mecanisme { get; set; } = string.Empty;

    // Route of administration for each drug — part of the DCI×Voie matching rule.
    // Empty string = legacy record (matches any voie for backward compatibility).
    [StringLength(100)]
    public string voie1 { get; set; } = string.Empty;

    [StringLength(100)]
    public string voie2 { get; set; } = string.Empty;

    public DateTime? addedat { get; set; }
    public DateTime? updatedat { get; set; }
}
