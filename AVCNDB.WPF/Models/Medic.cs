using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVCNDB.WPF.Models;

/// <summary>
/// Médicament - Entité principale
/// Contient toutes les informations d'un médicament
/// </summary>
[Table("medic")]
public class Medic : ITrackable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int recordid { get; set; }

    // ============================================
    // IDENTIFICATION
    // ============================================
    public int medicno { get; set; }
    
    [StringLength(20)]
    public string medicid { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string barcode { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string pctcode { get; set; } = string.Empty;
    
    [StringLength(30)]
    public string amm { get; set; } = string.Empty;

    // ============================================
    // DÉNOMINATION
    // ============================================
    [Required]
    [StringLength(150)]
    public string itemname { get; set; } = string.Empty;
    
    [StringLength(80)]
    public string shortname { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string basename { get; set; } = string.Empty;

    // ============================================
    // FORME & VOIE
    // ============================================
    [StringLength(50)]
    public string forme { get; set; } = string.Empty;
    
    [StringLength(40)]
    public string voie { get; set; } = string.Empty;
    
    [StringLength(25)]
    public string formgroup { get; set; } = string.Empty;
    
    public int groupid { get; set; }

    // ============================================
    // PRÉSENTATION & CONDITIONNEMENT
    // ============================================
    [StringLength(80)]
    public string present { get; set; } = string.Empty;
    
    public int colisage { get; set; }
    
    [StringLength(200)]
    public string posology { get; set; } = string.Empty;

    // ============================================
    // COMPOSITION (DCI)
    // ============================================
    [StringLength(100)]
    public string dci1 { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string dci2 { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string dci3 { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string dci4 { get; set; } = string.Empty;
    
    [StringLength(400)]
    public string dci { get; set; } = string.Empty;

    // ============================================
    // CLASSIFICATION
    // ============================================
    [StringLength(80)]
    public string fam1 { get; set; } = string.Empty;
    
    [StringLength(80)]
    public string fam2 { get; set; } = string.Empty;
    
    [StringLength(80)]
    public string fam3 { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string family { get; set; } = string.Empty;
    
    [StringLength(60)]
    public string specialite { get; set; } = string.Empty;

    // ============================================
    // PROPRIÉTÉS SPÉCIALES
    // ============================================
    public int pediatric { get; set; }
    
    [StringLength(20)]
    public string veic { get; set; } = string.Empty;
    
    public int isap { get; set; }
    public int isic { get; set; }

    // ============================================
    // TARIFICATION
    // ============================================
    public int price { get; set; }
    public int refprice { get; set; }
    public int ictx { get; set; }
    public int icamount { get; set; }
    public int ocamount { get; set; }
    public int pamount { get; set; }
    
    [StringLength(10)]
    public string tableau { get; set; } = string.Empty;
    
    public int pctprice { get; set; }
    public int timbrepct { get; set; }
    public int netprice { get; set; }

    // ============================================
    // LABORATOIRE
    // ============================================
    [StringLength(100)]
    public string labo { get; set; } = string.Empty;

    // ============================================
    // DOSAGE
    // ============================================
    [StringLength(20)]
    public string dose1 { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string dose2 { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string dose3 { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string dose4 { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string dose5 { get; set; } = string.Empty;

    // ============================================
    // UNITÉS
    // ============================================
    [StringLength(10)]
    public string u1 { get; set; } = string.Empty;
    
    [StringLength(10)]
    public string u2 { get; set; } = string.Empty;
    
    [StringLength(10)]
    public string u3 { get; set; } = string.Empty;
    
    [StringLength(10)]
    public string u4 { get; set; } = string.Empty;
    
    [StringLength(10)]
    public string u5 { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string ucol { get; set; } = string.Empty;
    
    public int unite { get; set; }

    // ============================================
    // INFORMATIONS MÉDICALES
    // ============================================
    [StringLength(150)]
    public string nameform { get; set; } = string.Empty;
    
    [StringLength(2000)]
    public string indication { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string mgarde { get; set; } = string.Empty;

    // ============================================
    // DATES & STATUT
    // ============================================
    public DateTime monogat { get; set; }
    public DateTime ciat { get; set; }
    public DateTime? addedat { get; set; }
    public DateTime? updatedat { get; set; }
    public DateTime? deletedat { get; set; }
    
    public int isactive { get; set; } = 1;
    
    [StringLength(10)]
    public string rowtype { get; set; } = string.Empty;
    
    public int itemtype { get; set; }
    
    [StringLength(50)]
    public string tatouage { get; set; } = string.Empty;
    
    public int isotc { get; set; }
}
