using System.Reflection;
using FuzzySharp;

namespace AVCNDB.WPF.Helpers;

/// <summary>
/// Maps Excel column headers (CNAM/legacy formats, French labels) to Medic/Dci/etc. property names.
/// Used by ExcelService when importing files whose headers don't exactly match property names.
///
/// Resolution order:
///   1. Exact match (case-insensitive) against property name
///   2. Exact match against any registered alias for that property
///   3. Fuzzy match (token-sort-ratio &gt;= 85) against property name + aliases
/// </summary>
public static class ColumnAliasMap
{
    private const int FuzzyThreshold = 85;

    /// <summary>Property name → list of accepted column-header aliases.</summary>
    private static readonly Dictionary<string, string[]> _aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        // CNAM Tunisia VEI format
        ["pctcode"]   = new[] { "code_pct", "code pct", "code", "codepct", "pct code" },
        ["itemname"]  = new[] { "nom_commercial", "nom commercial", "denomination", "dénomination",
                                "désignation", "designation", "nom du médicament", "nom medicament",
                                "drug name", "commercial name" },
        ["pamount"]   = new[] { "prix_public", "prix public", "ppv", "prix de vente",
                                "prix vente public", "public price" },
        ["price"]     = new[] { "prix_fab", "prix fabricant", "prix fab. ht", "manufacturer price" },
        ["refprice"]  = new[] { "tarif_reference", "tarif référence", "tarif reference",
                                "prix hospitalier", "prix réf.", "prix ref", "reference price" },
        ["pctprice"]  = new[] { "prix_pct", "prix pct", "prix de gros", "prix gros", "wholesale price" },
        ["timbrepct"] = new[] { "timbre", "timbre pct" },
        ["netprice"]  = new[] { "prix_net", "prix base remb.", "prix base rembourse", "prix net", "net price" },
        ["veic"]      = new[] { "categorie", "catégorie", "categorie veic", "classe veic", "veic class", "category" },
        ["dci1"]      = new[] { "dci", "dci principal", "dci_principal", "principal dci", "active ingredient" },
        ["dci2"]      = new[] { "dci secondaire", "dci_secondaire", "second dci" },
        ["isap"]      = new[] { "ap", "accord prealable", "accord préalable", "approval" },
        ["isic"]      = new[] { "ic", "remboursable", "reimbursable" },
        ["isotc"]     = new[] { "otc", "over the counter" },
        ["pediatric"] = new[] { "pédiatrique", "pediatrique", "pediatric", "enfant" },
        ["barcode"]   = new[] { "code_barre", "code-barre", "code barre", "ean", "ean13" },
        ["amm"]       = new[] { "n° amm", "n amm", "numero amm", "numéro amm", "marketing authorization" },
        ["labo"]      = new[] { "laboratoire", "lab", "laboratory", "labo." },
        ["forme"]     = new[] { "forme pharmaceutique", "forme pharm", "form" },
        ["voie"]      = new[] { "voie d'administration", "voie administration", "route" },
        ["present"]   = new[] { "présentation", "presentation", "packaging" },
        ["family"]    = new[] { "famille", "famille thérapeutique", "famille therapeutique", "therapeutic family" },
        ["specialite"]= new[] { "spécialité", "specialite", "speciality" },
        ["tableau"]   = new[] { "tableau", "schedule" },
        ["colisage"]  = new[] { "colis.", "colisage", "package size", "qty per pack" },
        ["ucol"]      = new[] { "unité col.", "unite col", "unite", "package unit" },
        ["dose1"]     = new[] { "dose", "dose 1", "dose1", "dosage" },
        ["u1"]        = new[] { "unité", "unite", "u1", "unit" },
        ["indication"]= new[] { "indications", "notes", "remarques", "note" },
        ["mgarde"]    = new[] { "mise en garde", "warning", "warnings" },
    };

    /// <summary>
    /// Resolve a column header (from Excel) to a property on type T.
    /// Returns null if no acceptable match (above fuzzy threshold) is found.
    /// </summary>
    public static PropertyInfo? Resolve(string header, IReadOnlyList<PropertyInfo> properties)
    {
        if (string.IsNullOrWhiteSpace(header) || properties.Count == 0) return null;

        var trimmed = header.Trim();

        // 1. Exact match (case-insensitive) against property name
        var exact = properties.FirstOrDefault(p =>
            p.Name.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
        if (exact != null) return exact;

        // 2. Exact match against an alias
        foreach (var (propName, aliases) in _aliases)
        {
            if (aliases.Any(a => a.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                var prop = properties.FirstOrDefault(p =>
                    p.Name.Equals(propName, StringComparison.OrdinalIgnoreCase));
                if (prop != null) return prop;
            }
        }

        // 3. Fuzzy match against property name OR any of its aliases
        var upper = trimmed.ToUpperInvariant();
        PropertyInfo? bestProp = null;
        int bestScore = 0;
        foreach (var p in properties)
        {
            var candidates = new List<string> { p.Name };
            if (_aliases.TryGetValue(p.Name, out var aliases))
                candidates.AddRange(aliases);

            foreach (var cand in candidates)
            {
                var score = Fuzz.TokenSortRatio(upper, cand.ToUpperInvariant());
                if (score > bestScore)
                {
                    bestScore = score;
                    bestProp = p;
                }
            }
        }

        return bestScore >= FuzzyThreshold ? bestProp : null;
    }

    /// <summary>For diagnostics — returns aliases registered for a given property.</summary>
    public static IReadOnlyList<string> GetAliases(string propertyName)
    {
        return _aliases.TryGetValue(propertyName, out var a)
            ? a
            : Array.Empty<string>();
    }
}
