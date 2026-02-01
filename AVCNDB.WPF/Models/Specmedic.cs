using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVCNDB.WPF.Models;

/// <summary>
/// Spécialités médicales liées aux médicaments
/// </summary>
[Table("specmedic")]
public class Specmedic : ITrackable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int recordid { get; set; }
    
    public int medicid { get; set; }
    public int specid { get; set; }
    
    [StringLength(150)]
    public string medicname { get; set; } = string.Empty;
    
    [StringLength(80)]
    public string specname { get; set; } = string.Empty;
    
    public DateTime? addedat { get; set; }
    public DateTime? updatedat { get; set; }
}
