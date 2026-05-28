using BCrypt.Net;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.DAL;
using AVCNDB.WPF.Models;
using Serilog;

namespace AVCNDB.WPF.Services;

/// <summary>
/// Implémentation du service d'authentification.
/// Utilise BCrypt.Net-Next (work factor 11) pour le hachage des mots de passe.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public AuthService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    /// <inheritdoc/>
    public async Task<SignInResult> SignInAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return new SignInResult(false, null, "Identifiant ou mot de passe manquant.", false);

        try
        {
            // Use a single tracked context: read, verify, update last_login, save — one round-trip.
            using var ctx = await _factory.CreateDbContextAsync();
            var normalised = username.Trim().ToLowerInvariant();
            var user = await ctx.Users
                .FirstOrDefaultAsync(u => u.username == normalised);

            if (user == null || !user.is_active)
                return new SignInResult(false, null, "Identifiant ou mot de passe incorrect.", false);

            var valid = BCrypt.Net.BCrypt.Verify(password, user.password_hash);
            if (!valid)
                return new SignInResult(false, null, "Identifiant ou mot de passe incorrect.", false);

            user.last_login = DateTime.Now;
            await ctx.SaveChangesAsync();

            Log.Information("Sign-in successful for user {Username}", user.username);
            return new SignInResult(true, user, null, user.must_change_password);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "SignInAsync failed for user {Username}", username);
            return new SignInResult(false, null, "Erreur interne lors de la connexion.", false);
        }
    }

    /// <inheritdoc/>
    public async Task<RememberMeIssueResult?> IssueRememberMeTokenAsync(int userId)
    {
        try
        {
            using var ctx = await _factory.CreateDbContextAsync();
            var user = await ctx.Users.FindAsync(userId);
            if (user == null || !user.is_active)
                return null;

            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var token = Convert.ToBase64String(tokenBytes);
            var expiresUtc = DateTime.UtcNow.AddDays(14);

            user.remember_token_hash = BCrypt.Net.BCrypt.HashPassword(token, workFactor: 11);
            user.remember_token_expires_utc = expiresUtc;
            user.updatedat = DateTime.Now;
            await ctx.SaveChangesAsync();

            Log.Information("Remember-me token issued for user {Username}", user.username);
            return new RememberMeIssueResult(token, expiresUtc);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "IssueRememberMeTokenAsync failed for userId {UserId}", userId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<User?> SignInFromRememberMeTokenAsync(string username, string token)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var normalised = username.Trim().ToLowerInvariant();
            using var ctx = await _factory.CreateDbContextAsync();
            var user = await ctx.Users.FirstOrDefaultAsync(u => u.username == normalised);

            if (user == null || !user.is_active)
                return null;
            if (string.IsNullOrWhiteSpace(user.remember_token_hash)
                || user.remember_token_expires_utc == null
                || user.remember_token_expires_utc <= DateTime.UtcNow)
            {
                await RevokeRememberMeTokenAsync(ctx, user);
                return null;
            }
            if (!BCrypt.Net.BCrypt.Verify(token, user.remember_token_hash))
            {
                Log.Warning("Invalid remember-me token for user {Username}", user.username);
                return null;
            }

            user.last_login = DateTime.Now;
            await ctx.SaveChangesAsync();

            Log.Information("Remember-me sign-in successful for user {Username}", user.username);
            return user;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "SignInFromRememberMeTokenAsync failed for user {Username}", username);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task RevokeRememberMeTokenAsync(int userId)
    {
        try
        {
            using var ctx = await _factory.CreateDbContextAsync();
            var user = await ctx.Users.FindAsync(userId);
            if (user == null) return;
            await RevokeRememberMeTokenAsync(ctx, user);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "RevokeRememberMeTokenAsync failed for userId {UserId}", userId);
        }
    }

    private static async Task RevokeRememberMeTokenAsync(AppDbContext ctx, User user)
    {
        user.remember_token_hash = null;
        user.remember_token_expires_utc = null;
        user.updatedat = DateTime.Now;
        await ctx.SaveChangesAsync();
        Log.Information("Remember-me token revoked for user {Username}", user.username);
    }

    /// <inheritdoc/>
    public Task SignOutAsync()
    {
        // Session clearing is handled by ISessionService — nothing to do at DB level.
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            return false;

        try
        {
            using var ctx = await _factory.CreateDbContextAsync();
            var user = await ctx.Users.FindAsync(userId);
            if (user == null) return false;

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.password_hash))
                return false;

            user.password_hash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 11);
            user.must_change_password = false;
            user.remember_token_hash = null;
            user.remember_token_expires_utc = null;
            user.updatedat = DateTime.Now;
            await ctx.SaveChangesAsync();

            Log.Information("Password changed for user {Username}", user.username);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ChangePasswordAsync failed for userId {UserId}", userId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<User> RegisterUserAsync(string username, string displayname, string password)
    {
        using var ctx = await _factory.CreateDbContextAsync();

        var user = new User
        {
            username = username.Trim().ToLowerInvariant(),
            displayname = displayname.Trim(),
            password_hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11),
            is_active = true,
            must_change_password = true,
            addedat = DateTime.Now,
        };

        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        Log.Information("New user registered: {Username}", user.username);
        return user;
    }
}
