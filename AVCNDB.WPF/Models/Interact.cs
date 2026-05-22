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
    
    [StringLength(30)]
    public string level { get; set; } = string.Empty;

    // Free-form clinical text. TEXT (no length limit) — the AI sometimes produces
    // verbose responses and silent truncation hid critical safety info.
    [Column(TypeName = "TEXT")]
    public string description { get; set; } = string.Empty;

    [Column(TypeName = "TEXT")]
    public string conduite { get; set; } = string.Empty;

    [Column(TypeName = "TEXT")]
    public string mecanisme { get; set; } = string.Empty;

    // Route of administration for each drug — part of the DCI×Voie matching rule.
    // Empty string = legacy record (matches any voie for backward compatibility).
    [StringLength(100)]
    public string voie1 { get; set; } = string.Empty;

    [StringLength(100)]
    public string voie2 { get; set; } = string.Empty;

    // Provenance: "manual" (legacy / human-curated) or "ai" (approved AI suggestion).
    // Lets the UI tag rows and supports future re-validation when the model evolves.
    [StringLength(20)]
    public string source { get; set; } = "manual";

    // AI model identifier that produced this row (empty for manual entries).
    [StringLength(100)]
    public string model { get; set; } = string.Empty;

    public DateTime? addedat { get; set; }
    public DateTime? updatedat { get; set; }
}
