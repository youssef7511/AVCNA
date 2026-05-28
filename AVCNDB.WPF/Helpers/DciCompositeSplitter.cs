namespace AVCNDB.WPF.Helpers;

/// <summary>
/// Décompose une dénomination DCI composée en ses principes actifs élémentaires.
///
/// Dans le référentiel, une association comme « ACIDE ACETYLSALICYLIQUE+CAFEINE »
/// désigne en réalité DEUX substances actives distinctes. La règle métier retenue :
/// le séparateur « + » sépare des DCI indépendantes, qui doivent vivre comme des
/// lignes séparées dans la table <c>dci</c> (jamais comme une seule entrée composée).
///
/// Ce découpage est appliqué à chaque point d'entrée d'une DCI dans le référentiel :
///   - ajout manuel (DciListViewModel) ;
///   - import / détection floue (EditionFileService, FuzzyDetectionService).
/// </summary>
public static class DciCompositeSplitter
{
    private static readonly char[] Separators = { '+' };

    /// <summary>
    /// Retourne les segments DCI distincts d'une dénomination.
    /// « ACIDE ACETYLSALICYLIQUE+CAFEINE » → [ "ACIDE ACETYLSALICYLIQUE", "CAFEINE" ].
    /// Une dénomination simple (sans « + ») renvoie un seul segment (elle-même, trimée).
    /// Les segments vides et les doublons canoniques sont éliminés ; l'ordre d'apparition
    /// est préservé.
    /// </summary>
    public static IReadOnlyList<string> Split(string? denomination)
    {
        if (string.IsNullOrWhiteSpace(denomination))
            return System.Array.Empty<string>();

        var seen = new HashSet<string>();
        var result = new List<string>();

        foreach (var part in denomination.Split(Separators, System.StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = part.Trim();
            if (trimmed.Length == 0) continue;

            var canon = NameNormalizer.Canonical(trimmed);
            if (canon.Length == 0) continue;
            if (seen.Add(canon))
                result.Add(trimmed);
        }

        return result;
    }

    /// <summary>
    /// Vrai si la dénomination contient au moins un séparateur « + » porteur de
    /// deux segments non vides (donc une vraie association à scinder).
    /// </summary>
    public static bool IsComposite(string? denomination)
        => Split(denomination).Count > 1;
}
