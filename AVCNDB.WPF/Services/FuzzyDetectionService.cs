using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.DAL;
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

            // Short-circuit: exact match
            if (score == 100) break;
        }

        result.Score = bestScore;
        result.BestMatch = bestMatch;
        result.IsKnown = bestScore >= _knownThreshold;

        return result;
    }

    /// <inheritdoc />
    public async Task<DetectionReport> DetectAsync(EditionRow row)
    {
        var reports = await DetectBatchAsync(new[] { row });
        return reports[0];
    }

    /// <inheritdoc />
    public async Task<List<DetectionReport>> DetectBatchAsync(IReadOnlyList<EditionRow> rows)
    {
        // Charger la bibliothèque une seule fois
        var library = await LoadLibraryAsync();
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
    /// Charge toutes les valeurs de référence des tables de bibliothèque
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

        var formes = await context.Formes
            .Select(f => f.itemname)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToListAsync();

        var voies = await context.Voies
            .Select(v => v.itemname)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToListAsync();

        var specialites = await context.Specialites
            .Select(s => s.itemname)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToListAsync();

        return new LibrarySnapshot(dcis, labos, families, formes, voies, specialites);
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
