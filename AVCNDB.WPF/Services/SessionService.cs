using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.Services;

/// <summary>
/// Singleton portant l'utilisateur connecté pour toute la durée de la session.
/// </summary>
public class SessionService : ISessionService
{
    private User? _currentUser;

    /// <inheritdoc/>
    public User? CurrentUser => _currentUser;

    /// <inheritdoc/>
    public bool IsAuthenticated => _currentUser != null;

    /// <inheritdoc/>
    public event EventHandler? SignedOut;

    /// <inheritdoc/>
    public void SetCurrentUser(User user)
    {
        _currentUser = user;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _currentUser = null;
        SignedOut?.Invoke(this, EventArgs.Empty);
    }
}
