using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Helpers;
using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.Services;

/// <summary>
/// Contrôle bidirectionnel entre les posologies générées dans le dialogue
/// Médicament et la table <c>poso</c> : tout enregistrement saisi côté Medic
/// est répliqué dans <c>poso</c> s'il n'existe pas déjà (comparaison par
/// normalisation canonique, accents et casse ignorés).
///
/// Sert également de source pour la combobox « Poso usuelle ».
/// </summary>
public class PosoLookupService
{
    private readonly IRepository<Poso> _posoRepo;

    public PosoLookupService(IRepository<Poso> posoRepo)
    {
        _posoRepo = posoRepo;
    }

    /// <summary>
    /// Récupère la ligne Poso dont <c>itemname</c> correspond exactement
    /// à <paramref name="denomination"/>. Retourne null si introuvable.
    /// </summary>
    public async Task<Poso?> FindByDenominationAsync(string denomination)
    {
        if (string.IsNullOrWhiteSpace(denomination)) return null;
        var matches = await _posoRepo.FindAsync(p => p.itemname == denomination);
        return matches.FirstOrDefault();
    }

    /// <summary>
    /// Liste triée des dénominations <c>poso.itemname</c> non vides,
    /// utilisée pour alimenter la combobox « Poso usuelle » du dialogue Medic.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetAllDenominationsAsync()
    {
        var all = await _posoRepo.GetAllAsync();
        return all
            .Select(p => p.itemname)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n)
            .ToList();
    }

    /// <summary>
    /// Indique si une dénomination canoniquement équivalente existe déjà
    /// dans <c>poso.itemname</c> (insensible à la casse et aux accents).
    /// </summary>
    public async Task<bool> ExistsAsync(string denomination)
    {
        if (string.IsNullOrWhiteSpace(denomination)) return false;
        var canonical = NameNormalizer.Canonical(denomination);
        var all = await _posoRepo.GetAllAsync();
        return all.Any(p => NameNormalizer.Canonical(p.itemname) == canonical);
    }

    /// <summary>
    /// Insère <paramref name="poso"/> dans la table <c>poso</c> si aucune
    /// ligne canoniquement équivalente (par <see cref="Poso.itemname"/>) n'y
    /// figure déjà. Aucun effet sinon.
    /// </summary>
    public async Task EnsureAsync(Poso poso)
    {
        if (poso == null) return;
        if (string.IsNullOrWhiteSpace(poso.itemname)) return;

        var canonical = NameNormalizer.Canonical(poso.itemname);
        var all = await _posoRepo.GetAllAsync();
        var exists = all.Any(p => NameNormalizer.Canonical(p.itemname) == canonical);
        if (exists) return;

        // Coalesce string fields pour éviter NOT NULL constraint violations.
        poso.itemname = poso.itemname.Trim();
        poso.periode ??= string.Empty;
        poso.conditions ??= string.Empty;
        poso.nameformul ??= string.Empty;
        poso.subvalue ??= string.Empty;
        // posoform peut rester null (FK ON DELETE SET NULL).
        poso.addedat = DateTime.Now;

        await _posoRepo.AddAsync(poso);
    }
}
