using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVCNDB.WPF.Models;

/// <summary>
/// Utilisateur administrateur de l'application AVCNDB
/// </summary>
[Table("user")]
public class User : ITrackable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int recordid { get; set; }

    /// <summary>Identifiant de connexion (unique, minuscule).</summary>
    [StringLength(60), Required]
    public string username { get; set; } = string.Empty;

    /// <summary>Nom affiché dans l'interface (optionnel).</summary>
    [StringLength(120)]
    public string displayname { get; set; } = string.Empty;

    /// <summary>Hash BCrypt du mot de passe (~60 chars).</summary>
    [StringLength(120), Required]
    public string password_hash { get; set; } = string.Empty;

    /// <summary>Indique si ce compte est actif.</summary>
    public bool is_active { get; set; } = true;

    /// <summary>Force un changement de mot de passe à la prochaine connexion.</summary>
    public bool must_change_password { get; set; } = false;

    /// <summary>Horodatage de la dernière connexion réussie.</summary>
    public DateTime? last_login { get; set; }

    /// <summary>Hash BCrypt du jeton persistant "Se souvenir de moi".</summary>
    [StringLength(120)]
    public string? remember_token_hash { get; set; }

    /// <summary>Date d'expiration UTC du jeton persistant.</summary>
    public DateTime? remember_token_expires_utc { get; set; }

    public DateTime? addedat { get; set; }
    public DateTime? updatedat { get; set; }
}
