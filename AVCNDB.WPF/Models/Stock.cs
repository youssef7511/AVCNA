using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVCNDB.WPF.Models;

/// <summary>
/// Stock des médicaments
/// </summary>
[Table("stock")]
public class Stock : ITrackable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int recordid { get; set; }
    
    public int medicid { get; set; }
    
    [StringLength(150)]
    public string medicname { get; set; } = string.Empty;
    
    public int quantity { get; set; }
    
    public int minstock { get; set; }
    
    public int maxstock { get; set; }
    
    public DateTime? expirydate { get; set; }
    
    [StringLength(50)]
    public string batchno { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string location { get; set; } = string.Empty;
    
    public int isalerted { get; set; }
    
    public DateTime? addedat { get; set; }
    public DateTime? updatedat { get; set; }
}
