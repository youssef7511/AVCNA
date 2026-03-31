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

            // Sync Présentation
            if (!string.IsNullOrWhiteSpace(medic.present))
            {
                var presentName = medic.present.Trim();
                var exists = await ctx.Presents.AnyAsync(p => p.itemname == presentName);
                if (!exists)
                {
                    ctx.Presents.Add(new Presents { itemname = presentName, addedat = DateTime.Now });
                    _logger.LogInformation("Auto-ajout Présentation: {Name}", presentName);
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
    /// Renomme une DCI dans la table des interactions (champs dci1, dci2).
    /// </summary>
    public async Task<int> RenameDciInInteractionsAsync(string oldName, string newName)
    {
        if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)) return 0;
        if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase)) return 0;

        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var interactions = await ctx.Interacts
                .Where(i => i.dci1 == oldName || i.dci2 == oldName)
                .ToListAsync();

            foreach (var i in interactions)
            {
                if (i.dci1 == oldName) i.dci1 = newName;
                if (i.dci2 == oldName) i.dci2 = newName;
                i.updatedat = DateTime.Now;
            }

            await ctx.SaveChangesAsync();
            _logger.LogInformation(
                "DCI renommee '{Old}' -> '{New}' dans {Count} interaction(s)",
                oldName, newName, interactions.Count);
            return interactions.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du renommage DCI interactions '{Old}' -> '{New}'", oldName, newName);
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
    /// Efface une DCI de la table des interactions (avant suppression).
    /// </summary>
    public async Task<int> ClearDciFromInteractionsAsync(string dciName)
    {
        if (string.IsNullOrWhiteSpace(dciName)) return 0;

        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var interactions = await ctx.Interacts
                .Where(i => i.dci1 == dciName || i.dci2 == dciName)
                .ToListAsync();

            foreach (var i in interactions)
            {
                if (i.dci1 == dciName) i.dci1 = string.Empty;
                if (i.dci2 == dciName) i.dci2 = string.Empty;
                i.updatedat = DateTime.Now;
            }

            await ctx.SaveChangesAsync();
            _logger.LogInformation("DCI '{Name}' effacee de {Count} interaction(s)", dciName, interactions.Count);
            return interactions.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'effacement DCI interactions '{Name}'", dciName);
            return 0;
        }
    }

    /// <summary>
    /// Applique les substituts DCI: pour chaque ligne DCI avec subvalue renseigne,
    /// remplace itemname par subvalue et propage dans medic (dci1..dci4 + dci) et interact.
    /// </summary>
    public async Task<DciSubstituteApplyResult> ApplyDciSubstitutsAsync()
    {
        var result = new DciSubstituteApplyResult();

        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();

            var candidates = await ctx.Dcis
                .Where(d => !string.IsNullOrWhiteSpace(d.subvalue) && d.itemname != d.subvalue)
                .ToListAsync();

            result.CandidateCount = candidates.Count;
            if (candidates.Count == 0)
            {
                return result;
            }

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var d in candidates)
            {
                var oldName = d.itemname?.Trim();
                var newName = d.subvalue?.Trim();
                if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)) continue;
                map[oldName] = newName;
            }

            if (map.Count == 0)
            {
                return result;
            }

            var oldNames = map.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var now = DateTime.Now;

            foreach (var d in candidates)
            {
                var oldName = d.itemname;
                if (!map.TryGetValue(oldName, out var newName)) continue;

                d.itemname = newName;
                d.subvalue = string.Empty;
                if (!string.IsNullOrWhiteSpace(d.iteminfo))
                {
                    d.iteminfo = d.iteminfo.Replace(oldName, newName, StringComparison.OrdinalIgnoreCase);
                }
                d.updatedat = now;
                result.UpdatedDciCount++;
            }

            var medics = await ctx.Medics
                .Where(m => oldNames.Contains(m.dci1) || oldNames.Contains(m.dci2) ||
                            oldNames.Contains(m.dci3) || oldNames.Contains(m.dci4))
                .ToListAsync();

            foreach (var m in medics)
            {
                var changed = false;
                if (TryMapDciValue(m.dci1, map, out var v1)) { m.dci1 = v1; changed = true; }
                if (TryMapDciValue(m.dci2, map, out var v2)) { m.dci2 = v2; changed = true; }
                if (TryMapDciValue(m.dci3, map, out var v3)) { m.dci3 = v3; changed = true; }
                if (TryMapDciValue(m.dci4, map, out var v4)) { m.dci4 = v4; changed = true; }

                if (!changed) continue;

                m.dci = BuildCombinedDci(m);
                m.updatedat = now;
                result.UpdatedMedicCount++;
            }

            var interactions = await ctx.Interacts
                .Where(i => oldNames.Contains(i.dci1) || oldNames.Contains(i.dci2))
                .ToListAsync();

            foreach (var i in interactions)
            {
                var changed = false;
                if (TryMapDciValue(i.dci1, map, out var nv1)) { i.dci1 = nv1; changed = true; }
                if (TryMapDciValue(i.dci2, map, out var nv2)) { i.dci2 = nv2; changed = true; }

                if (!changed) continue;

                i.updatedat = now;
                result.UpdatedInteractCount++;
            }

            await ctx.SaveChangesAsync();
            result.AppliedCount = map.Count;

            _logger.LogInformation(
                "Application substituts DCI terminee. DCI={Dci}, Medic={Medic}, Interact={Interact}",
                result.UpdatedDciCount, result.UpdatedMedicCount, result.UpdatedInteractCount);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'application des substituts DCI");
            result.ErrorMessage = ex.Message;
            return result;
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

    // ----- PRÉSENTATIONS -----

    /// <summary>
    /// Renomme une présentation dans tous les médicaments.
    /// </summary>
    public async Task<int> RenamePresentInMedicsAsync(string oldName, string newName)
    {
        if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)) return 0;
        if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase)) return 0;

        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var medics = await ctx.Medics.Where(m => m.present == oldName).ToListAsync();

            foreach (var m in medics)
            {
                m.present = newName;
                m.updatedat = DateTime.Now;
            }

            await ctx.SaveChangesAsync();
            _logger.LogInformation("Présentation renommée '{Old}' → '{New}' dans {Count} médicaments", oldName, newName, medics.Count);
            return medics.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du renommage Présentation '{Old}' → '{New}'", oldName, newName);
            return 0;
        }
    }

    /// <summary>
    /// Efface la référence à une présentation de tous les médicaments.
    /// </summary>
    public async Task<int> ClearPresentFromMedicsAsync(string presentName)
    {
        if (string.IsNullOrWhiteSpace(presentName)) return 0;

        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var medics = await ctx.Medics.Where(m => m.present == presentName).ToListAsync();

            foreach (var m in medics)
            {
                m.present = string.Empty;
                m.updatedat = DateTime.Now;
            }

            await ctx.SaveChangesAsync();
            _logger.LogInformation("Présentation '{Name}' effacée de {Count} médicaments", presentName, medics.Count);
            return medics.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'effacement Présentation '{Name}'", presentName);
            return 0;
        }
    }

    /// <summary>
    /// Compte les médicaments utilisant cette présentation.
    /// </summary>
    public async Task<int> CountMedicsUsingPresentAsync(string presentName)
    {
        if (string.IsNullOrWhiteSpace(presentName)) return 0;

        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.Medics.CountAsync(m => m.present == presentName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur comptage Présentation '{Name}'", presentName);
            return 0;
        }
    }

    // ================================================================
    // HELPERS
    // ================================================================

    /// <summary>
    /// Reconstruit le champ combiné 'dci' à partir de dci1..dci4.
    /// Public pour être accessible depuis d'autres services/VMs.
    /// </summary>
    public static string BuildCombinedDci(Medic m)
    {
        var parts = new[] { m.dci1, m.dci2, m.dci3, m.dci4 }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim());
        return string.Join(" + ", parts);
    }

    private static bool TryMapDciValue(
        string currentValue,
        IReadOnlyDictionary<string, string> map,
        out string mappedValue)
    {
        mappedValue = currentValue;
        if (string.IsNullOrWhiteSpace(currentValue)) return false;
        if (!map.TryGetValue(currentValue.Trim(), out var v)) return false;
        mappedValue = v;
        return true;
    }

    // ================================================================
    // GENERIC "APPLY SUBSTITUTS" — For all library tables
    // ================================================================

    /// <summary>
    /// Applies substituts for Familles: replaces itemname with subvalue,
    /// then propagates to medic.fam1/fam2/fam3/family.
    /// </summary>
    public async Task<SubstituteApplyResult> ApplyFamiliesSubstitutsAsync()
    {
        return await ApplySubstitutsGenericAsync(
            ctx => ctx.Families,
            f => f.itemname,
            (f, v) => f.itemname = v,
            f => f.subvalue,
            (f, v) => f.subvalue = v,
            f => f.updatedat = DateTime.Now,
            async (ctx, map, oldNames, now) =>
            {
                var medics = await ctx.Medics
                    .Where(m => oldNames.Contains(m.fam1) || oldNames.Contains(m.fam2) ||
                                oldNames.Contains(m.fam3) || oldNames.Contains(m.family))
                    .ToListAsync();
                int count = 0;
                foreach (var m in medics)
                {
                    var changed = false;
                    if (map.TryGetValue(m.fam1, out var v1)) { m.fam1 = v1; changed = true; }
                    if (map.TryGetValue(m.fam2, out var v2)) { m.fam2 = v2; changed = true; }
                    if (map.TryGetValue(m.fam3, out var v3)) { m.fam3 = v3; changed = true; }
                    if (map.TryGetValue(m.family, out var v4)) { m.family = v4; changed = true; }
                    if (changed) { m.updatedat = now; count++; }
                }
                return count;
            },
            "Families");
    }

    /// <summary>
    /// Applies substituts for Labos: replaces itemname with subvalue,
    /// then propagates to medic.labo.
    /// </summary>
    public async Task<SubstituteApplyResult> ApplyLabosSubstitutsAsync()
    {
        return await ApplySubstitutsSingleFieldAsync<Labos>(
            ctx => ctx.Labos,
            l => l.itemname, (l, v) => l.itemname = v,
            l => l.subvalue, (l, v) => l.subvalue = v,
            l => l.updatedat = DateTime.Now,
            m => m.labo, (m, v) => m.labo = v,
            "Labos");
    }

    /// <summary>
    /// Applies substituts for Formes: replaces itemname with subvalue,
    /// then propagates to medic.forme.
    /// </summary>
    public async Task<SubstituteApplyResult> ApplyFormesSubstitutsAsync()
    {
        return await ApplySubstitutsSingleFieldAsync<Formes>(
            ctx => ctx.Formes,
            f => f.itemname, (f, v) => f.itemname = v,
            f => f.subvalue, (f, v) => f.subvalue = v,
            f => f.updatedat = DateTime.Now,
            m => m.forme, (m, v) => m.forme = v,
            "Formes");
    }

    /// <summary>
    /// Applies substituts for Voies: replaces itemname with subvalue,
    /// then propagates to medic.voie.
    /// </summary>
    public async Task<SubstituteApplyResult> ApplyVoiesSubstitutsAsync()
    {
        return await ApplySubstitutsSingleFieldAsync<Voies>(
            ctx => ctx.Voies,
            v => v.itemname, (vo, val) => vo.itemname = val,
            v => v.subvalue, (vo, val) => vo.subvalue = val,
            v => v.updatedat = DateTime.Now,
            m => m.voie, (m, val) => m.voie = val,
            "Voies");
    }

    /// <summary>
    /// Applies substituts for Presents: replaces itemname with subvalue,
    /// then propagates to medic.present.
    /// </summary>
    public async Task<SubstituteApplyResult> ApplyPresentsSubstitutsAsync()
    {
        return await ApplySubstitutsSingleFieldAsync<Presents>(
            ctx => ctx.Presents,
            p => p.itemname, (p, v) => p.itemname = v,
            p => p.subvalue, (p, v) => p.subvalue = v,
            p => p.updatedat = DateTime.Now,
            m => m.present, (m, v) => m.present = v,
            "Presents");
    }

    /// <summary>
    /// Applies substituts for Specialites: replaces itemname with subvalue,
    /// then propagates to medic.specialite.
    /// </summary>
    public async Task<SubstituteApplyResult> ApplySpecialitesSubstitutsAsync()
    {
        return await ApplySubstitutsSingleFieldAsync<Specialites>(
            ctx => ctx.Specialites,
            s => s.itemname, (s, v) => s.itemname = v,
            s => s.subvalue, (s, v) => s.subvalue = v,
            s => s.updatedat = DateTime.Now,
            m => m.specialite, (m, v) => m.specialite = v,
            "Specialites");
    }

    /// <summary>
    /// Applies substituts for Poso: replaces itemname with subvalue.
    /// (Poso has no direct field in Medic, so only the entity table is updated.)
    /// </summary>
    public async Task<SubstituteApplyResult> ApplyPosoSubstitutsAsync()
    {
        return await ApplySubstitutsGenericAsync(
            ctx => ctx.Posos,
            p => p.itemname,
            (p, v) => p.itemname = v,
            p => p.subvalue,
            (p, v) => p.subvalue = v,
            p => p.updatedat = DateTime.Now,
            (_, _, _, _) => Task.FromResult(0), // no medic propagation
            "Poso");
    }

    /// <summary>
    /// Generic helper for entities that map to a single medic string field.
    /// </summary>
    private async Task<SubstituteApplyResult> ApplySubstitutsSingleFieldAsync<TEntity>(
        Func<AppDbContext, DbSet<TEntity>> getDbSet,
        Func<TEntity, string> getItemName,
        Action<TEntity, string> setItemName,
        Func<TEntity, string> getSubValue,
        Action<TEntity, string> setSubValue,
        Action<TEntity> touchTimestamp,
        Func<Medic, string> getMedicField,
        Action<Medic, string> setMedicField,
        string entityLabel) where TEntity : class
    {
        return await ApplySubstitutsGenericAsync(
            getDbSet, getItemName, setItemName, getSubValue, setSubValue, touchTimestamp,
            async (ctx, map, oldNames, now) =>
            {
                // Load all medics and filter in-memory (delegate can't translate to SQL)
                var allMedics = await ctx.Medics.ToListAsync();
                var matching = allMedics
                    .Where(m => oldNames.Contains(getMedicField(m) ?? ""))
                    .ToList();

                int count = 0;
                foreach (var m in matching)
                {
                    var oldVal = getMedicField(m)?.Trim() ?? "";
                    if (map.TryGetValue(oldVal, out var newVal))
                    {
                        setMedicField(m, newVal);
                        m.updatedat = now;
                        count++;
                    }
                }
                return count;
            },
            entityLabel);
    }

    /// <summary>
    /// Generic apply-substituts engine. Handles entity table rename + optional medic propagation.
    /// </summary>
    private async Task<SubstituteApplyResult> ApplySubstitutsGenericAsync<TEntity>(
        Func<AppDbContext, DbSet<TEntity>> getDbSet,
        Func<TEntity, string> getItemName,
        Action<TEntity, string> setItemName,
        Func<TEntity, string> getSubValue,
        Action<TEntity, string> setSubValue,
        Action<TEntity> touchTimestamp,
        Func<AppDbContext, Dictionary<string, string>, HashSet<string>, DateTime, Task<int>> propagateToMedics,
        string entityLabel) where TEntity : class
    {
        var result = new SubstituteApplyResult();
        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var dbSet = getDbSet(ctx);
            var all = await dbSet.ToListAsync();

            var candidates = all
                .Where(e =>
                {
                    var sub = getSubValue(e)?.Trim();
                    var name = getItemName(e)?.Trim();
                    return !string.IsNullOrWhiteSpace(sub) && sub != name;
                })
                .ToList();

            result.CandidateCount = candidates.Count;
            if (candidates.Count == 0) return result;

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in candidates)
            {
                var oldName = getItemName(e)?.Trim();
                var newName = getSubValue(e)?.Trim();
                if (!string.IsNullOrWhiteSpace(oldName) && !string.IsNullOrWhiteSpace(newName))
                    map[oldName] = newName;
            }

            if (map.Count == 0) return result;

            var now = DateTime.Now;
            foreach (var e in candidates)
            {
                var newName = map.GetValueOrDefault(getItemName(e)?.Trim() ?? "");
                if (newName == null) continue;
                setItemName(e, newName);
                setSubValue(e, string.Empty);
                touchTimestamp(e);
                result.UpdatedEntityCount++;
            }

            var oldNames = map.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            result.UpdatedMedicCount = await propagateToMedics(ctx, map, oldNames, now);

            await ctx.SaveChangesAsync();
            result.AppliedCount = map.Count;

            _logger.LogInformation(
                "Substituts {Entity} appliqués. Entités={Ent}, Médic={Med}",
                entityLabel, result.UpdatedEntityCount, result.UpdatedMedicCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur application substituts {Entity}", entityLabel);
            result.ErrorMessage = ex.Message;
        }
        return result;
    }
}

public sealed class DciSubstituteApplyResult
{
    public int CandidateCount { get; set; }
    public int AppliedCount { get; set; }
    public int UpdatedDciCount { get; set; }
    public int UpdatedMedicCount { get; set; }
    public int UpdatedInteractCount { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class SubstituteApplyResult
{
    public int CandidateCount { get; set; }
    public int AppliedCount { get; set; }
    public int UpdatedEntityCount { get; set; }
    public int UpdatedMedicCount { get; set; }
    public string? ErrorMessage { get; set; }
}
