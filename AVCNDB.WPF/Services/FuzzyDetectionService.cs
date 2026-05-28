using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.DAL;
using AVCNDB.WPF.Helpers;
using AVCNDB.WPF.Models;
using FuzzySharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AVCNDB.WPF.Services;

/// <summary>
/// Service de détection de données inconnues via FuzzySharp (token-sort-ratio).
/// Seuil configurable via AppSettings:FuzzyThreshold (défaut: 80).
/// </summary>
public class FuzzyDetectionService : IUnknownDataDetectionService
{
    private readonly int _knownThreshold;

    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    /// <summary>
    /// Instantané de bibliothèque mis en cache, réutilisé par la re-validation
    /// champ-par-champ à l'édition (évite une requête SQL à chaque frappe).
    /// </summary>
    private LibrarySnapshot? _cachedLibrary;

    public FuzzyDetectionService(IDbContextFactory<AppDbContext> contextFactory, IConfiguration configuration)
    {
        _contextFactory = contextFactory;
        _knownThreshold = configuration.GetValue("AppSettings:FuzzyThreshold", 80);
    }

    /// <inheritdoc />
    public FieldDetectionResult CheckValue(string fieldName, string value, IReadOnlyList<string> knownValues)
    {
        var result = new FieldDetectionResult
        {
            FieldName = fieldName,
            ImportedValue = value
        };

        // Valeur vide = toujours connue (pas de faux positif sur champs optionnels)
        if (string.IsNullOrWhiteSpace(value))
        {
            result.IsKnown = true;
            result.Score = 100;
            return result;
        }

        // Pas de référence = inconnu
        if (knownValues.Count == 0)
        {
            result.IsKnown = false;
            result.Score = 0;
            return result;
        }

        // DCI composées : « X+Y » désigne deux principes actifs. La valeur n'est
        // considérée comme connue que si CHAQUE segment l'est ; le score retenu
        // est celui du segment le plus faible (maillon faible de l'association).
        var isDciField = fieldName == "Dci" || fieldName == "DciAssociation";
        if (isDciField && DciCompositeSplitter.IsComposite(value))
        {
            var segments = DciCompositeSplitter.Split(value);
            int worstScore = 100;
            string? worstMatch = null;
            var allKnown = true;
            foreach (var seg in segments)
            {
                var (segScore, segMatch) = MatchSingle(seg, knownValues);
                if (segScore < worstScore) { worstScore = segScore; worstMatch = segMatch; }
                if (segScore < _knownThreshold) allKnown = false;
            }
            result.Score = worstScore;
            result.BestMatch = worstMatch;
            result.IsKnown = allKnown;
            return result;
        }

        var (bestScore, bestMatch) = MatchSingle(value, knownValues);
        result.Score = bestScore;
        result.BestMatch = bestMatch;
        result.IsKnown = bestScore >= _knownThreshold;

        return result;
    }

    /// <summary>
    /// Meilleur score de correspondance floue d'une valeur unique contre la
    /// liste des valeurs connues. Retourne (score, valeur connue la plus proche).
    /// </summary>
    private static (int score, string? match) MatchSingle(string value, IReadOnlyList<string> knownValues)
    {
        var upperValue = value.ToUpperInvariant();
        int bestScore = 0;
        string? bestMatch = null;

        foreach (var known in knownValues)
        {
            if (string.IsNullOrWhiteSpace(known)) continue;

            var score = Fuzz.TokenSortRatio(upperValue, known.ToUpperInvariant());
            if (score > bestScore)
            {
                bestScore = score;
                bestMatch = known;
            }

            if (score == 100) break; // exact match
        }

        return (bestScore, bestMatch);
    }

    /// <inheritdoc />
    public async Task<DetectionReport> DetectAsync(EditionRow row)
    {
        var reports = await DetectBatchAsync(new[] { row });
        return reports[0];
    }

    /// <inheritdoc />
    public async Task<bool> IsFieldKnownAsync(string fieldName, string? value)
    {
        // Champ vide = pas d'alerte (cohérent avec CheckValue).
        if (string.IsNullOrWhiteSpace(value)) return true;

        var library = await GetLibraryAsync();
        var known = GetKnownValuesFor(fieldName, library);
        // Champ non soumis à détection (ex: Tableau, Veic) → jamais signalé inconnu.
        if (known == null) return true;

        return CheckValue(fieldName, value, known).IsKnown;
    }

    /// <inheritdoc />
    public void InvalidateLibraryCache() => _cachedLibrary = null;

    /// <summary>
    /// Retourne l'instantané de bibliothèque mis en cache, le chargeant au besoin.
    /// </summary>
    private async Task<LibrarySnapshot> GetLibraryAsync(bool forceReload = false)
    {
        if (forceReload || _cachedLibrary == null)
            _cachedLibrary = await LoadLibraryAsync();
        return _cachedLibrary;
    }

    /// <summary>
    /// Associe un nom de champ de détection à la liste de valeurs connues
    /// correspondante. Doit rester aligné avec le mapping de <see cref="BuildReport"/>.
    /// </summary>
    private static IReadOnlyList<string>? GetKnownValuesFor(string fieldName, LibrarySnapshot lib) => fieldName switch
    {
        "Dci" or "DciAssociation" => lib.Dcis,
        "Labo"                    => lib.Labos,
        "Fam1" or "Fam2" or "Fam3" => lib.Families,
        "Forme"                   => lib.Formes,
        "Voie"                    => lib.Voies,
        "Specialite"              => lib.Specialites,
        _                         => null
    };

    /// <inheritdoc />
    public async Task<List<DetectionReport>> DetectBatchAsync(IReadOnlyList<EditionRow> rows)
    {
        // Charger la bibliothèque une seule fois (et rafraîchir le cache partagé
        // avec la re-validation à l'édition).
        var library = await GetLibraryAsync(forceReload: true);
        var reports = new List<DetectionReport>(rows.Count);

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var report = BuildReport(i, row, library);
            reports.Add(report);
        }

        return reports;
    }

    /// <summary>
    /// Charge toutes les valeurs de référence des tables de bibliothèque.
    /// Objectif 3 : pour les tables qui portent un champ d'abréviation
    /// (Formes, Voies et Spécialités), les deux colonnes (itemname ET abname)
    /// sont concaténées dans la liste comparée. Ainsi un fichier Excel
    /// contenant « COMP » fera matcher la forme « Comprimé » via son abname.
    /// </summary>
    private async Task<LibrarySnapshot> LoadLibraryAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var dcis = await context.Dcis
            .Select(d => d.itemname)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToListAsync();

        var labos = await context.Labos
            .Select(l => l.itemname)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToListAsync();

        var families = await context.Families
            .Select(f => f.itemname)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToListAsync();

        var formesRows = await context.Formes
            .Select(f => new { f.itemname, f.abname })
            .ToListAsync();
        var formes = MergeNameAndAbbreviation(
            formesRows.Select(r => ((string?)r.itemname, (string?)r.abname)));

        var voiesRows = await context.Voies
            .Select(v => new { v.itemname, v.abname })
            .ToListAsync();
        var voies = MergeNameAndAbbreviation(
            voiesRows.Select(r => ((string?)r.itemname, (string?)r.abname)));

        var specialitesRows = await context.Specialites
            .Select(s => new { s.itemname, s.abname })
            .ToListAsync();
        var specialites = MergeNameAndAbbreviation(
            specialitesRows.Select(r => ((string?)r.itemname, (string?)r.abname)));

        return new LibrarySnapshot(dcis, labos, families, formes, voies, specialites);
    }

    /// <summary>
    /// Fusionne les colonnes itemname et abname en une liste unique sans
    /// doublons. Une valeur de fichier Excel matchera fuzzy contre l'une ou
    /// l'autre. Le mapping vers l'entité réelle se fait en aval par requête
    /// SQL via les deux colonnes.
    /// </summary>
    private static List<string> MergeNameAndAbbreviation(IEnumerable<(string? itemname, string? abname)> rows)
    {
        var merged = new List<string>();
        foreach (var (itemname, abname) in rows)
        {
            if (!string.IsNullOrWhiteSpace(itemname)) merged.Add(itemname.Trim());
            if (!string.IsNullOrWhiteSpace(abname)) merged.Add(abname.Trim());
        }
        return merged.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Construit un rapport de détection pour une ligne en vérifiant chaque champ
    /// </summary>
    private DetectionReport BuildReport(int rowIndex, EditionRow row, LibrarySnapshot library)
    {
        var report = new DetectionReport { RowIndex = rowIndex };

        // DCI (dci1 est le champ principal)
        report.FieldResults.Add(CheckValue("Dci", row.Dci1, library.Dcis));

        // DCI-Association (champ composite dci)
        report.FieldResults.Add(CheckValue("DciAssociation", row.DciAssociation, library.Dcis));

        // Laboratoire
        report.FieldResults.Add(CheckValue("Labo", row.Labo, library.Labos));

        // Familles
        report.FieldResults.Add(CheckValue("Fam1", row.Fam1, library.Families));
        report.FieldResults.Add(CheckValue("Fam2", row.Fam2, library.Families));
        report.FieldResults.Add(CheckValue("Fam3", row.Fam3, library.Families));

        // Forme
        report.FieldResults.Add(CheckValue("Forme", row.Forme, library.Formes));

        // Voie
        report.FieldResults.Add(CheckValue("Voie", row.Voie, library.Voies));

        // Spécialité
        report.FieldResults.Add(CheckValue("Specialite", row.Specialite, library.Specialites));

        return report;
    }

    /// <summary>
    /// Snapshot de toutes les valeurs de bibliothèque chargées en mémoire
    /// </summary>
    private record LibrarySnapshot(
        IReadOnlyList<string> Dcis,
        IReadOnlyList<string> Labos,
        IReadOnlyList<string> Families,
        IReadOnlyList<string> Formes,
        IReadOnlyList<string> Voies,
        IReadOnlyList<string> Specialites
    );
}
