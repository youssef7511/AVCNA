using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.Services;

/// <summary>
/// Source unique de vérité pour l'auto-remplissage du champ « Forme poso »
/// (abréviation) lors de la sélection d'une Forme galénique dans le dialogue
/// Médicament. Évite tout switch/case dispersé ou hardcode dans les ViewModels.
///
/// Convention : on lit <see cref="Formes.posoform"/> (champ libre saisi par
/// l'admin dans la Bibliothèque → Formes). Si vide ou Forme introuvable, on
/// retombe sur le nom complet de la Forme.
/// </summary>
public class FormePosoAbbreviationService
{
    private readonly IRepository<Formes> _formesRepo;

    public FormePosoAbbreviationService(IRepository<Formes> formesRepo)
    {
        _formesRepo = formesRepo;
    }

    /// <summary>
    /// Retourne l'abréviation associée à <paramref name="formeName"/> :
    /// 1. <see cref="Formes.posoform"/> si renseignée ;
    /// 2. <paramref name="formeName"/> lui-même (fallback) sinon ;
    /// 3. chaîne vide si l'entrée est null ou blanche.
    /// </summary>
    public async Task<string> GetFormAbbreviationAsync(string? formeName)
    {
        if (string.IsNullOrWhiteSpace(formeName)) return string.Empty;
        var trimmed = formeName.Trim();
        var matches = await _formesRepo.FindAsync(f => f.itemname == trimmed);
        var match = matches.FirstOrDefault();
        return !string.IsNullOrWhiteSpace(match?.posoform)
            ? match!.posoform!.Trim()
            : trimmed; // fallback explicite : nom complet
    }
}
