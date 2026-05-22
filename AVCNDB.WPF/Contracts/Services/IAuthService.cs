using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.Contracts.Services;

/// <summary>Résultat d'une tentative de connexion.</summary>
public record SignInResult(bool Success, User? User, string? ErrorMessage, bool MustChangePassword);

/// <summary>Contrat du service d'authentification.</summary>
public interface IAuthService
{
    /// <summary>Vérifie les identifiants et ouvre la session.</summary>
    Task<SignInResult> SignInAsync(string username, string password);

    /// <summary>
    /// Connexion via token Remember-Me : pas de vérification de mot de passe,
    /// vérifie uniquement que le compte est actif et met à jour last_login.
    /// </summary>
    Task<User?> SignInFromTokenAsync(string username);

    /// <summary>Ferme la session.</summary>
    Task SignOutAsync();

    /// <summary>
    /// Change le mot de passe d'un utilisateur après vérification du mot de passe actuel.
    /// Réinitialise must_change_password à false en cas de succès.
    /// </summary>
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

    /// <summary>Crée un nouveau compte administrateur.</summary>
    Task<User> RegisterUserAsync(string username, string displayname, string password);
}
