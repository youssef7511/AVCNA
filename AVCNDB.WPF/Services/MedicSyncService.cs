using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AVCNDB.WPF.DAL;
using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.Services;

/// <summary>
/// Service de synchronisation bidirectionnelle entre les médicaments 
/// et les tables de référence (DCI, Familles, Labos, Formes, Voies).
/// 
/// Direction 1 : Medic → Lookups (quand on sauvegarde un médicament, 
///   les valeurs sont ajoutées aux tables de référence si absentes)
/// Direction 2 : Lookups → Medics (quand on renomme/supprime une entrée 
///   de référence, tous les médicaments sont mis à jour)
/// </summary>
public class MedicSyncService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<MedicSyncService> _logger;

    public MedicSyncService(
        IDbContextFactory<AppDbContext> contextFactory,
        ILogger<MedicSyncService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    // ================================================================
    // DIRECTION 1 : Medic → Lookup Tables
    // Quand on sauvegarde un médicament, on s'assure que les valeurs 
    // sont présentes dans les tables de référence
    // ================================================================

    /// <summary>
    /// Synchronise les tables de référence à partir des champs d'un médicament.
    /// Ajoute les valeurs manquantes dans DCI, Familles, Labos, Formes, Voies.
    /// </summary>
    public async Task SyncLookupTablesAsync(Medic medic)
    {
        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();

            // Sync DCI (dci1..dci4)
            var dciValues = new[] { medic.dci1, medic.dci2, medic.dci3, medic.dci4 }
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var dciName in dciValues)
            {
                var exists = await ctx.Dcis.AnyAsync(d => d.itemname == dciName);
                if (!exists)
                {
                    ctx.Dcis.Add(new Dci { itemname = dciName, addedat = DateTime.Now });
                    _logger.LogInformation("Auto-ajout DCI: {Name}", dciName);
                }
            }

            // Sync Familles (fam1, fam2, fam3, family)
            var famValues = new[] { medic.fam1, medic.fam2, medic.fam3, medic.family }
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var famName in famValues)
            {
                var exists = await ctx.Families.AnyAsync(f => f.itemname == famName);
                if (!exists)
                {
                    ctx.Families.Add(new Families { itemname = famName, addedat = DateTime.Now });
                    _logger.LogInformation("Auto-ajout Famille: {Name}", famName);
                }
            }

            // Sync Labo
            if (!string.IsNullOrWhiteSpace(medic.labo))
            {
                var laboName = medic.labo.Trim();
                var exists = await ctx.Labos.AnyAsync(l => l.itemname == laboName);
                if (!exists)
                {
                    ctx.Labos.Add(new Labos { itemname = laboName, addedat = DateTime.Now });
                    _logger.LogInformation("Auto-ajout Labo: {Name}", laboName);
                }
            }

            // Sync Forme
            if (!string.IsNullOrWhiteSpace(medic.forme))
            {
                var formeName = medic.forme.Trim();
                var exists = await ctx.Formes.AnyAsync(f => f.itemname == formeName);
                if (!exists)
                {
                    ctx.Formes.Add(new Formes { itemname = formeName, addedat = DateTime.Now });
                    _logger.LogInformation("Auto-ajout Forme: {Name}", formeName);
                }
            }

            // Sync Voie
            if (!string.IsNullOrWhiteSpace(medic.voie))
            {
                var voieName = medic.voie.Trim();
                var exists = await ctx.Voies.AnyAsync(v => v.itemname == voieName);
                if (!exists)
                {
                    ctx.Voies.Add(new Voies { itemname = voieName, addedat = DateTime.Now });
                    _logger.LogInformation("Auto-ajout Voie: {Name}", voieName);
                }
            }

            await ctx.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la synchronisation Medic → Lookups");
        }
    }

    // ================================================================
    // DIRECTION 2 : Lookup Tables → Medics
    // Quand on renomme ou supprime une entrée de référence,
    // on propage le changement à tous les médicaments concernés
    // ================================================================

    // ----- DCI -----

    /// <summary>
    /// Renomme une DCI dans tous les médicaments (champs dci1..dci4 + dci combiné).
    /// </summary>
    public async Task<int> RenameDciInMedicsAsync(string oldName, string newName)
    {
        if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)) return 0;
        if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase)) return 0;

        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var medics = await ctx.Medics
                .Where(m => m.dci1 == oldName || m.dci2 == oldName || 
                            m.dci3 == oldName || m.dci4 == oldName)
                .ToListAsync();

            foreach (var m in medics)
            {
                if (m.dci1 == oldName) m.dci1 = newName;
                if (m.dci2 == oldName) m.dci2 = newName;
                if (m.dci3 == oldName) m.dci3 = newName;
                if (m.dci4 == oldName) m.dci4 = newName;
                m.dci = BuildCombinedDci(m);
                m.updatedat = DateTime.Now;
            }

            await ctx.SaveChangesAsync();
            _logger.LogInformation("DCI renommée '{Old}' → '{New}' dans {Count} médicaments", oldName, newName, medics.Count);
            return medics.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du renommage DCI '{Old}' → '{New}'", oldName, newName);
            return 0;
        }
    }

    /// <summary>
    /// Efface une DCI des champs de tous les médicaments (avant suppression).
    /// </summary>
    public async Task<int> ClearDciFromMedicsAsync(string dciName)
    {
        if (string.IsNullOrWhiteSpace(dciName)) return 0;

        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var medics = await ctx.Medics
                .Where(m => m.dci1 == dciName || m.dci2 == dciName || 
                            m.dci3 == dciName || m.dci4 == dciName)
                .ToListAsync();

            foreach (var m in medics)
            {
                if (m.dci1 == dciName) m.dci1 = string.Empty;
                if (m.dci2 == dciName) m.dci2 = string.Empty;
                if (m.dci3 == dciName) m.dci3 = string.Empty;
                if (m.dci4 == dciName) m.dci4 = string.Empty;
                m.dci = BuildCombinedDci(m);
                m.updatedat = DateTime.Now;
            }

            await ctx.SaveChangesAsync();
            _logger.LogInformation("DCI '{Name}' effacée de {Count} médicaments", dciName, medics.Count);
            return medics.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'effacement DCI '{Name}'", dciName);
            return 0;
        }
    }

    /// <summary>
    /// Compte les médicaments utilisant cette DCI.
    /// </summary>
    public async Task<int> CountMedicsUsingDciAsync(string dciName)
    {
        if (string.IsNullOrWhiteSpace(dciName)) return 0;

        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.Medics.CountAsync(m => 
                m.dci1 == dciName || m.dci2 == dciName || 
                m.dci3 == dciName || m.dci4 == dciName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur comptage DCI '{Name}'", dciName);
            return 0;
        }
    }

    // ----- LABOS -----

    /// <summary>
    /// Renomme un laboratoire dans tous les médicaments.
    /// </summary>
    public async Task<int> RenameLaboInMedicsAsync(string oldName, string newName)
    {
        if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)) return 0;
        if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase)) return 0;

        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var medics = await ctx.Medics.Where(m => m.labo == oldName).ToListAsync();

            foreach (var m in medics)
            {
                m.labo = newName;
                m.updatedat = DateTime.Now;
            }

            await ctx.SaveChangesAsync();
            _logger.LogInformation("Labo renommé '{Old}' → '{New}' dans {Count} médicaments", oldName, newName, medics.Count);
            return medics.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du renommage Labo '{Old}' → '{New}'", oldName, newName);
            return 0;
        }
    }

    /// <summary>
    /// Efface un laboratoire des champs de tous les médicaments.
    /// </summary>
    public async Task<int> ClearLaboFromMedicsAsync(string laboName)
    {
        if (string.IsNullOrWhiteSpace(laboName)) return 0;

        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var medics = await ctx.Medics.Where(m => m.labo == laboName).ToListAsync();

            foreach (var m in medics)
            {
                m.labo = string.Empty;
                m.updatedat = DateTime.Now;
            }

            await ctx.SaveChangesAsync();
            _logger.LogInformation("Labo '{Name}' effacé de {Count} médicaments", laboName, medics.Count);
            return medics.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'effacement Labo '{Name}'", laboName);
            return 0;
        }
    }

    /// <summary>
    /// Compte les médicaments utilisant ce laboratoire.
    /// </summary>
    public async Task<int> CountMedicsUsingLaboAsync(string laboName)
    {
        if (string.IsNullOrWhiteSpace(laboName)) return 0;

        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.Medics.CountAsync(m => m.labo == laboName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur comptage Labo '{Name}'", laboName);
            return 0;
        }
    }

    // ----- FAMILLES -----

    /// <summary>
    /// Renomme une famille dans tous les médicaments (champs fam1, fam2, fam3, family).
    /// </summary>
    public async Task<int> RenameFamilyInMedicsAsync(string oldName, string newName)
    {
        if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)) return 0;
        if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase)) return 0;

        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var medics = await ctx.Medics
                .Where(m => m.fam1 == oldName || m.fam2 == oldName || 
                            m.fam3 == oldName || m.family == oldName)
                .ToListAsync();

            foreach (var m in medics)
            {
                if (m.fam1 == oldName) m.fam1 = newName;
                if (m.fam2 == oldName) m.fam2 = newName;
                if (m.fam3 == oldName) m.fam3 = newName;
                if (m.family == oldName) m.family = newName;
                m.updatedat = DateTime.Now;
            }

            await ctx.SaveChangesAsync();
            _logger.LogInformation("Famille renommée '{Old}' → '{New}' dans {Count} médicaments", oldName, newName, medics.Count);
            return medics.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du renommage Famille '{Old}' → '{New}'", oldName, newName);
            return 0;
        }
    }

    /// <summary>
    /// Efface une famille des champs de tous les médicaments.
    /// </summary>
    public async Task<int> ClearFamilyFromMedicsAsync(string familyName)
    {
        if (string.IsNullOrWhiteSpace(familyName)) return 0;

        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var medics = await ctx.Medics
                .Where(m => m.fam1 == familyName || m.fam2 == familyName || 
                            m.fam3 == familyName || m.family == familyName)
                .ToListAsync();

            foreach (var m in medics)
            {
                if (m.fam1 == familyName) m.fam1 = string.Empty;
                if (m.fam2 == familyName) m.fam2 = string.Empty;
                if (m.fam3 == familyName) m.fam3 = string.Empty;
                if (m.family == familyName) m.family = string.Empty;
                m.updatedat = DateTime.Now;
            }

            await ctx.SaveChangesAsync();
            _logger.LogInformation("Famille '{Name}' effacée de {Count} médicaments", familyName, medics.Count);
            return medics.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'effacement Famille '{Name}'", familyName);
            return 0;
        }
    }

    /// <summary>
    /// Compte les médicaments utilisant cette famille.
    /// </summary>
    public async Task<int> CountMedicsUsingFamilyAsync(string familyName)
    {
        if (string.IsNullOrWhiteSpace(familyName)) return 0;

        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.Medics.CountAsync(m => 
                m.fam1 == familyName || m.fam2 == familyName || 
                m.fam3 == familyName || m.family == familyName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur comptage Famille '{Name}'", familyName);
            return 0;
        }
    }

    // ----- FORMES -----

    /// <summary>
    /// Renomme une forme dans tous les médicaments.
    /// </summary>
    public async Task<int> RenameFormeInMedicsAsync(string oldName, string newName)
    {
        if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)) return 0;
        if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase)) return 0;

        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var medics = await ctx.Medics.Where(m => m.forme == oldName).ToListAsync();

            foreach (var m in medics)
            {
                m.forme = newName;
                m.updatedat = DateTime.Now;
            }

            await ctx.SaveChangesAsync();
            _logger.LogInformation("Forme renommée '{Old}' → '{New}' dans {Count} médicaments", oldName, newName, medics.Count);
            return medics.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du renommage Forme '{Old}' → '{New}'", oldName, newName);
            return 0;
        }
    }

    /// <summary>
    /// Efface une forme des champs de tous les médicaments.
    /// </summary>
    public async Task<int> ClearFormeFromMedicsAsync(string formeName)
    {
        if (string.IsNullOrWhiteSpace(formeName)) return 0;

        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var medics = await ctx.Medics.Where(m => m.forme == formeName).ToListAsync();

            foreach (var m in medics)
            {
                m.forme = string.Empty;
                m.updatedat = DateTime.Now;
            }

            await ctx.SaveChangesAsync();
            _logger.LogInformation("Forme '{Name}' effacée de {Count} médicaments", formeName, medics.Count);
            return medics.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'effacement Forme '{Name}'", formeName);
            return 0;
        }
    }

    /// <summary>
    /// Compte les médicaments utilisant cette forme.
    /// </summary>
    public async Task<int> CountMedicsUsingFormeAsync(string formeName)
    {
        if (string.IsNullOrWhiteSpace(formeName)) return 0;

        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.Medics.CountAsync(m => m.forme == formeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur comptage Forme '{Name}'", formeName);
            return 0;
        }
    }

    // ----- VOIES -----

    /// <summary>
    /// Renomme une voie d'administration dans tous les médicaments.
    /// </summary>
    public async Task<int> RenameVoieInMedicsAsync(string oldName, string newName)
    {
        if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)) return 0;
        if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase)) return 0;

        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var medics = await ctx.Medics.Where(m => m.voie == oldName).ToListAsync();

            foreach (var m in medics)
            {
                m.voie = newName;
                m.updatedat = DateTime.Now;
            }

            await ctx.SaveChangesAsync();
            _logger.LogInformation("Voie renommée '{Old}' → '{New}' dans {Count} médicaments", oldName, newName, medics.Count);
            return medics.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du renommage Voie '{Old}' → '{New}'", oldName, newName);
            return 0;
        }
    }

    /// <summary>
    /// Efface une voie des champs de tous les médicaments.
    /// </summary>
    public async Task<int> ClearVoieFromMedicsAsync(string voieName)
    {
        if (string.IsNullOrWhiteSpace(voieName)) return 0;

        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var medics = await ctx.Medics.Where(m => m.voie == voieName).ToListAsync();

            foreach (var m in medics)
            {
                m.voie = string.Empty;
                m.updatedat = DateTime.Now;
            }

            await ctx.SaveChangesAsync();
            _logger.LogInformation("Voie '{Name}' effacée de {Count} médicaments", voieName, medics.Count);
            return medics.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'effacement Voie '{Name}'", voieName);
            return 0;
        }
    }

    /// <summary>
    /// Compte les médicaments utilisant cette voie.
    /// </summary>
    public async Task<int> CountMedicsUsingVoieAsync(string voieName)
    {
        if (string.IsNullOrWhiteSpace(voieName)) return 0;

        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.Medics.CountAsync(m => m.voie == voieName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur comptage Voie '{Name}'", voieName);
            return 0;
        }
    }

    // ================================================================
    // HELPERS
    // ================================================================

    /// <summary>
    /// Reconstruit le champ combiné 'dci' à partir de dci1..dci4
    /// </summary>
    private static string BuildCombinedDci(Medic m)
    {
        var parts = new[] { m.dci1, m.dci2, m.dci3, m.dci4 }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim());
        return string.Join(" + ", parts);
    }
}
