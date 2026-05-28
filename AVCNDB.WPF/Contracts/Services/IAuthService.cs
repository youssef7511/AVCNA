using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.Contracts.Services;

/// <summary>Résultat d'une tentative de connexion.</summary>
public record SignInResult(bool Success, User? User, string? ErrorMessage, bool MustChangePassword);

/// <summary>Jeton persistant émis après une connexion par mot de passe réussie.</summary>
public record RememberMeIssueResult(string Token, DateTime ExpiresUtc);

/// <summary>Contrat du service d'authentification.</summary>
public interface IAuthService
{
    /// <summary>Vérifie les identifiants et ouvre la session.</summary>
    Task<SignInResult> SignInAsync(string username, string password);

    /// <summary>Émet un jeton persistant hashé en base pour "Se souvenir de moi".</summary>
    Task<RememberMeIssueResult?> IssueRememberMeTokenAsync(int userId);

    /// <summary>Connexion via jeton Remember-Me validé contre le hash stocké en base.</summary>
    Task<User?> SignInFromRememberMeTokenAsync(string username, string token);

    /// <summary>Révoque le jeton persistant d'un utilisateur.</summary>
    Task RevokeRememberMeTokenAsync(int userId);

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
