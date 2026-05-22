using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Serilog;

namespace AVCNDB.WPF.Services;

/// <summary>Token sérialisé dans auth.dat.</summary>
public record RememberMeToken(string Username, DateTime ExpiresUtc, string Nonce);

/// <summary>
/// Persistance "Se souvenir de moi" via Windows DPAPI.
/// Le fichier est chiffré avec DataProtectionScope.CurrentUser :
/// seul le compte Windows courant sur cette machine peut le déchiffrer.
/// </summary>
public class RememberMeService
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AVCNDB",
        "auth.dat");

    private static readonly TimeSpan Expiry = TimeSpan.FromDays(14);

    /// <summary>Chiffre et persiste le token.</summary>
    public void Save(string username, string nonce, DateTime expiresUtc)
    {
        try
        {
            var token = new RememberMeToken(username, expiresUtc, nonce);
            var json = JsonSerializer.Serialize(token);
            var plain = Encoding.UTF8.GetBytes(json);
            var cipher = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);

            var dir = Path.GetDirectoryName(FilePath)!;
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(FilePath, cipher);

            Log.Information("Remember-me token saved for user {Username}", username);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to save remember-me token");
        }
    }

    /// <summary>
    /// Crée et persiste un token valide 14 jours pour l'utilisateur donné.
    /// </summary>
    public void SaveForUser(string username)
    {
        var nonce = Guid.NewGuid().ToString("N");
        // Use UTC so the expiry is not affected by daylight-saving transitions.
        Save(username, nonce, DateTime.UtcNow.Add(Expiry));
    }

    /// <summary>
    /// Tente de charger et déchiffrer le token.
    /// Retourne false si le fichier est absent, le déchiffrement échoue, ou le token est expiré.
    /// </summary>
    public bool TryLoad(out RememberMeToken token)
    {
        token = default!;
        try
        {
            if (!File.Exists(FilePath)) return false;

            var cipher = File.ReadAllBytes(FilePath);
            var plain = ProtectedData.Unprotect(cipher, null, DataProtectionScope.CurrentUser);
            var json = Encoding.UTF8.GetString(plain);
            var t = JsonSerializer.Deserialize<RememberMeToken>(json);
            if (t == null) return false;

            if (t.ExpiresUtc < DateTime.UtcNow)
            {
                Log.Information("Remember-me token expired for user {Username}", t.Username);
                Clear();
                return false;
            }

            token = t;
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load remember-me token (DPAPI binding mismatch or file corrupt)");
            Clear();
            return false;
        }
    }

    /// <summary>Supprime le fichier token.</summary>
    public void Clear()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
                Log.Information("Remember-me token cleared");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to clear remember-me token");
        }
    }
}
