using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.Contracts.Services;

/// <summary>Contrat du service de session (singleton).</summary>
public interface ISessionService
{
    /// <summary>Utilisateur actuellement connecté, ou null si aucune session active.</summary>
    User? CurrentUser { get; }

    /// <summary>Indique si une session est ouverte.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Déclenché quand la session est fermée (déconnexion explicite).</summary>
    event EventHandler? SignedOut;

    /// <summary>Enregistre l'utilisateur connecté.</summary>
    void SetCurrentUser(User user);

    /// <summary>Vide la session et déclenche l'événement SignedOut.</summary>
    void Clear();
}
