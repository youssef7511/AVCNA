using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using FuzzySharp;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.DAL;
using AVCNDB.WPF.Helpers;
using AVCNDB.WPF.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AVCNDB.WPF.Services;

/// <summary>
/// Service de gestion du fichier d'édition.
/// Orchestre import Excel → détection ML → approbation/rejet → export.
/// </summary>
public class EditionFileService : IEditionFileService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IUnknownDataDetectionService _detectionService;
    private readonly IExcelService _excelService;

    public EditionFileService(
        IDbContextFactory<AppDbContext> contextFactory,
        IUnknownDataDetectionService detectionService,
        IExcelService excelService)
    {
        _contextFactory = contextFactory;
        _detectionService = detectionService;
        _excelService = excelService;
    }

    /// <inheritdoc />
    public async Task<ImportResult> ImportExcelAsync(string filePath, EditionSourceType sourceType)
    {
        var result = new ImportResult();

        try
        {
            var (rows, warnings, wasTransposed) = await Task.Run(() =>
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheets.First();
                var fieldAliases = GetFieldAliases(sourceType);

                // 1. Auto-detect orientation (horizontal vs vertical)
                bool transposed = DetectOrientation(worksheet, fieldAliases);

                // 2. If vertical, transpose to horizontal view
                if (transposed)
                {
                    worksheet = TransposeWorksheet(workbook, worksheet);
                    Log.Information("Orientation verticale détectée — transposition automatique du fichier {FilePath}", filePath);
                }

                // 3. Build column map with fuzzy matching
                var headerRow = worksheet.Row(1);
                var headerWarnings = new List<HeaderWarning>();
                var columnMap = BuildColumnMapFuzzy(headerRow, fieldAliases, headerWarnings);

                // 4. Parse data rows
                var editionRows = new List<EditionRow>();
                var dataRows = worksheet.RowsUsed().Skip(1); // skip header
                int lineNumber = 1;

                foreach (var xlRow in dataRows)
                {
                    var edRow = MapXlRowToEditionRow(xlRow, columnMap, lineNumber);
                    editionRows.Add(edRow);
                    lineNumber++;
                }

                return (editionRows, headerWarnings, transposed);
            });

            // Tenter de relier à des Medics existants par PctCode
            await MatchExistingMedicsAsync(rows);

            result.Success = true;
            result.Rows = rows;
            result.TotalRowsRead = rows.Count;
            result.WasTransposed = wasTransposed;
            result.HeaderWarnings = warnings;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erreur lors de l'import du fichier d'édition: {FilePath}", filePath);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <inheritdoc />
    public Task<bool> IsFieldKnownAsync(string fieldName, string value)
        => _detectionService.IsFieldKnownAsync(fieldName, value);

    /// <inheritdoc />
    public async Task<int> ValidateAgainstLibraryAsync(List<EditionRow> rows)
    {
        var reports = await _detectionService.DetectBatchAsync(rows);
        int unknownCount = 0;

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var report = reports[i];

            row.UnknownFields.Clear();
            row.UnknownFields.AddRange(report.UnknownFieldNames);
            row.NotifyUnknownFieldsChanged();

            if (!report.AllFieldsKnown)
            {
                if (row.ActionFlag == ActionFlag.None)
                {
                    row.ActionFlag = ActionFlag.AjouterNew;
                }
                unknownCount++;
            }
        }

        return unknownCount;
    }

    /// <inheritdoc />
    public async Task ApproveRowAsync(EditionRow row)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // MySqlRetryingExecutionStrategy (EnableRetryOnFailure) does not allow
        // user-initiated transactions directly. Wrapping with CreateExecutionStrategy
        // makes the transaction retriable and compatible with the retry policy.
        var strategy = context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                // Ajouter les valeurs inconnues aux tables de bibliothèque
                foreach (var fieldName in row.UnknownFields)
                {
                    var value = GetFieldValue(row, fieldName);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        await AddToLibraryAsync(fieldName, value);
                    }
                }

                if (row.OriginalMedicRecordId.HasValue)
                {
                    var medic = await context.Medics.FindAsync(row.OriginalMedicRecordId.Value);
                    if (medic != null)
                    {
                        UpdateMedicFromRow(medic, row);
                        context.Medics.Update(medic);
                    }
                }
                else
                {
                    // Vérifier les doublons avant insertion
                    var itemName = row.ItemName?.Trim();
                    var dci1 = row.Dci1?.Trim();
                    if (!string.IsNullOrWhiteSpace(itemName))
                    {
                        var exists = await context.Medics.AnyAsync(m =>
                            m.itemname == itemName &&
                            (string.IsNullOrEmpty(dci1) || m.dci1 == dci1));
                        if (exists)
                            throw new InvalidOperationException(
                                $"Un médicament '{itemName}' (DCI: {dci1 ?? "N/A"}) existe déjà.");
                    }

                    var newMedic = MapEditionRowToMedic(row);
                    context.Medics.Add(newMedic);
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                Log.Information("Ligne approuvée: {ItemName} (ID original: {OriginalId})",
                    row.ItemName, row.OriginalMedicRecordId);

                row.UnknownFields.Clear();
                row.NotifyUnknownFieldsChanged();
                row.ActionFlag = ActionFlag.Affecte;
                row.RowStatus = RowStatus.Modified;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });

        // L'approbation a pu ajouter de nouvelles valeurs aux tables de référence :
        // invalider le cache pour que la re-validation à l'édition les reconnaisse.
        _detectionService.InvalidateLibraryCache();
    }

    /// <inheritdoc />
    public async Task RejectRowAsync(EditionRow row)
    {
        await Task.CompletedTask;
        row.ActionFlag = ActionFlag.Desaffecte;
        Log.Information("Ligne rejetée: {ItemName}", row.ItemName);
        row.UnknownFields.Clear();
        row.NotifyUnknownFieldsChanged();
    }

    /// <inheritdoc />
    public async Task ExportEditionFileAsync(List<EditionRow> rows, string filePath)
    {
        await _excelService.ExportAsync(rows, filePath, "Edition");
    }

    /// <inheritdoc />
    public async Task SaveSessionAsync(EditionFileSession session)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.EditionFileSessions.Add(session);
        await context.SaveChangesAsync();
    }

    // ============================================
    // PRIVATE HELPERS
    // ============================================

    /// <summary>
    /// Construit le mapping colonnes Excel → champs EditionRow avec correspondance floue (fuzzy).
    /// - Exact match (case-insensitive) → direct mapping, pas d'avertissement
    /// - Fuzzy score ≥ 75 → auto-corrigé + avertissement FuzzyCorrected
    /// - En dessous → colonne ignorée + avertissement Unrecognized
    /// </summary>
    private static Dictionary<int, string> BuildColumnMapFuzzy(
        IXLRow headerRow,
        Dictionary<string, string> fieldAliases,
        List<HeaderWarning> warnings)
    {
        const int FuzzyHeaderThreshold = 75;
        var map = new Dictionary<int, string>();
        var lastCol = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
        var aliasKeys = fieldAliases.Keys.ToList();

        for (int col = 1; col <= lastCol; col++)
        {
            var header = headerRow.Cell(col).GetString().Trim();
            if (string.IsNullOrWhiteSpace(header))
                continue;

            // 1. Exact match (case-insensitive)
            if (fieldAliases.TryGetValue(header, out var fieldName))
            {
                map[col] = fieldName;
                continue;
            }

            // 2. Fuzzy match against all known aliases
            int bestScore = 0;
            string? bestAlias = null;

            foreach (var alias in aliasKeys)
            {
                int score = Fuzz.TokenSortRatio(header, alias);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestAlias = alias;
                }
            }

            if (bestScore >= FuzzyHeaderThreshold && bestAlias != null)
            {
                // Auto-correct typo
                var mappedField = fieldAliases[bestAlias];
                map[col] = mappedField;
                warnings.Add(new HeaderWarning
                {
                    Type = HeaderWarning.WarningType.FuzzyCorrected,
                    OriginalHeader = header,
                    CorrectedTo = bestAlias,
                    MappedField = mappedField,
                    FuzzyScore = bestScore
                });
                Log.Warning("En-tête corrigé par fuzzy: '{Original}' → '{Corrected}' (champ: {Field}, score: {Score})",
                    header, bestAlias, mappedField, bestScore);
            }
            else
            {
                // Unrecognized column
                warnings.Add(new HeaderWarning
                {
                    Type = HeaderWarning.WarningType.Unrecognized,
                    OriginalHeader = header,
                    FuzzyScore = bestScore
                });
                Log.Warning("En-tête non reconnu ignoré: '{Header}' (meilleur score: {Score})", header, bestScore);
            }
        }

        return map;
    }

    // ── Orientation Detection ──

    /// <summary>
    /// Détecte si les données sont en orientation horizontale (normal) ou verticale (transposée).
    /// Compare combien d'en-têtes connus on trouve en ligne 1 (horizontal) vs colonne A (vertical).
    /// </summary>
    private static bool DetectOrientation(IXLWorksheet ws, Dictionary<string, string> fieldAliases)
    {
        const int FuzzyOrientationThreshold = 70;
        int maxCheck = 20; // check first 20 cells in each direction

        // Count matches in Row 1 (horizontal — normal)
        int horizontalMatches = 0;
        int lastColRow1 = Math.Min(ws.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0, maxCheck);
        for (int col = 1; col <= lastColRow1; col++)
        {
            var text = ws.Row(1).Cell(col).GetString().Trim();
            if (!string.IsNullOrWhiteSpace(text) && IsKnownHeader(text, fieldAliases, FuzzyOrientationThreshold))
                horizontalMatches++;
        }

        // Count matches in Column A (vertical — transposed)
        int verticalMatches = 0;
        int lastRowColA = Math.Min(ws.Column(1).LastCellUsed()?.Address.RowNumber ?? 0, maxCheck);
        for (int row = 1; row <= lastRowColA; row++)
        {
            var text = ws.Column(1).Cell(row).GetString().Trim();
            if (!string.IsNullOrWhiteSpace(text) && IsKnownHeader(text, fieldAliases, FuzzyOrientationThreshold))
                verticalMatches++;
        }

        // Vertical only if clearly more headers found vertically and very few horizontally
        return verticalMatches > horizontalMatches && verticalMatches >= 3 && horizontalMatches <= 1;
    }

    /// <summary>
    /// Checks if a text matches any known header alias (exact or fuzzy).
    /// </summary>
    private static bool IsKnownHeader(string text, Dictionary<string, string> fieldAliases, int threshold)
    {
        if (fieldAliases.ContainsKey(text))
            return true;

        foreach (var alias in fieldAliases.Keys)
        {
            if (Fuzz.TokenSortRatio(text, alias) >= threshold)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Transpose une feuille verticale (headers en colonne A, données en colonnes B, C, ...)
    /// vers une feuille horizontale standard.
    /// </summary>
    private static IXLWorksheet TransposeWorksheet(XLWorkbook workbook, IXLWorksheet source)
    {
        var transposed = workbook.AddWorksheet("__transposed__");

        int lastRow = source.LastRowUsed()?.RowNumber() ?? 0;
        int lastCol = source.LastColumnUsed()?.ColumnNumber() ?? 0;

        for (int r = 1; r <= lastRow; r++)
        {
            for (int c = 1; c <= lastCol; c++)
            {
                var value = source.Cell(r, c).Value;
                transposed.Cell(c, r).Value = value;
            }
        }

        return transposed;
    }

    /// <summary>
    /// Retourne le dictionnaire d'alias colonnes → champs pour le type de source donné.
    /// OrdinalIgnoreCase — ne pas ajouter d'entrées qui ne diffèrent que par la casse.
    /// </summary>
    private static Dictionary<string, string> GetFieldAliases(EditionSourceType sourceType)
    {
        return sourceType switch
        {
            // ── CNAM : fichier nomenclature officiel (tarification nationale) ──
            EditionSourceType.ExcelCNAM => new(StringComparer.OrdinalIgnoreCase)
            {
                { "Code PCT", "PctCode" }, { "Code_PCT", "PctCode" }, { "CodePCT", "PctCode" },
                { "Dénomination", "ItemName" }, { "Denomination", "ItemName" },
                // Colonnes spécifiques fichiers CNAM exportés (NOM_COMMERCIAL, PRIX_PUBLIC, etc.)
                { "NOM_COMMERCIAL", "ItemName" }, { "NOM COMMERCIAL", "ItemName" },
                { "D.C.I", "Dci1" }, { "DCI", "Dci1" },
                { "D.C.J-Association", "DciAssociation" }, { "D.C.J Association", "DciAssociation" },
                { "DCI-Association", "DciAssociation" },
                { "Forme", "Forme" },
                { "Voie", "Voie" }, { "Voie d'administration", "Voie" },
                { "Tab.", "Tableau" }, { "Tab", "Tableau" },
                { "VEiC", "Veic" }, { "Catégorie VEIC", "Veic" },
                { "Labo.", "Labo" }, { "Labo", "Labo" }, { "Laboratoire", "Labo" },
                { "Famille", "Fam1" }, { "Fam", "Fam1" },
                { "CATEGORIE", "Fam1" }, { "Catégorie", "Fam1" },
                { "Spécialité", "Specialite" }, { "Specialite", "Specialite" }, { "Spécialite", "Specialite" },
                { "Prix-Réf", "RefPrice" }, { "Prix Réf", "RefPrice" }, { "Prix de référence", "RefPrice" },
                { "TARIF_REFERENCE", "RefPrice" }, { "TARIF REFERENCE", "RefPrice" }, { "TARIF_RÉFÉRENCE", "RefPrice" },
                { "Prix", "Price" }, { "Prix de vente public", "Price" },
                { "PRIX_PUBLIC", "PAmount" }, { "PRIX PUBLIC", "PAmount" }, { "PPV", "PAmount" },
                { "Remb.", "IsRemboursable" }, { "Remb", "IsRemboursable" },
                { "A.P.", "IsAp" }, { "A.P", "IsAp" }, { "AP", "IsAp" },
            },

            // ── PCT basique : extrait simple de la pharmacopée ──
            EditionSourceType.ExcelPCT => new(StringComparer.OrdinalIgnoreCase)
            {
                { "Code PCT", "PctCode" }, { "Code_PCT", "PctCode" }, { "CodePCT", "PctCode" },
                { "Dénomination", "ItemName" }, { "Denomination", "ItemName" }, { "Nom", "ItemName" },
                { "D.C.I", "Dci1" }, { "DCI", "Dci1" },
                { "Forme", "Forme" },
                { "Voie", "Voie" }, { "Voie d'administration", "Voie" },
                { "Labo.", "Labo" }, { "Labo", "Labo" }, { "Laboratoire", "Labo" },
                { "Famille", "Fam1" }, { "Fam", "Fam1" },
                { "Spécialité", "Specialite" }, { "Specialite", "Specialite" }, { "Spécialite", "Specialite" },
                { "Prix", "Price" },
            },

            // ── PCT Complet : extrait exhaustif (tous les champs) ──
            EditionSourceType.ExcelPCTComplete => new(StringComparer.OrdinalIgnoreCase)
            {
                { "Code PCT", "PctCode" }, { "Code_PCT", "PctCode" }, { "CodePCT", "PctCode" },
                { "Dénomination", "ItemName" }, { "Denomination", "ItemName" },
                { "Désignation", "ShortName" }, { "Designation", "ShortName" },
                { "D.C.I", "Dci1" }, { "DCI", "Dci1" },
                { "D.C.J-Association", "DciAssociation" }, { "D.C.J Association", "DciAssociation" },
                { "DCI-Association", "DciAssociation" }, { "Association", "DciAssociation" },
                { "Forme", "Forme" },
                { "Voie", "Voie" },
                { "Tab.", "Tableau" }, { "Tab", "Tableau" },
                { "VEiC", "Veic" }, { "Catégorie VEIC", "Veic" },
                { "Labo.", "Labo" }, { "Labo", "Labo" }, { "Laboratoire", "Labo" },
                { "Spécialité", "Specialite" },
                { "Famille", "Fam1" },
                { "Prix-Réf", "RefPrice" }, { "Prix Réf", "RefPrice" }, { "Prix de référence", "RefPrice" },
                { "Prix", "Price" }, { "Prix de vente public", "Price" },
                { "Remb.", "IsRemboursable" }, { "Remb", "IsRemboursable" },
                { "A.P.", "IsAp" }, { "A.P", "IsAp" }, { "Accord préalable", "IsAp" },
            },

            // ── Catalogue Labo : liste produits d'un laboratoire ──
            EditionSourceType.CatalogueLabo => new(StringComparer.OrdinalIgnoreCase)
            {
                { "Code", "PctCode" }, { "Réf", "PctCode" }, { "Référence", "PctCode" },
                { "Produit", "ItemName" }, { "Nom du produit", "ItemName" },
                { "Nom", "ItemName" }, { "Dénomination", "ItemName" },
                { "DCI", "Dci1" }, { "D.C.I", "Dci1" }, { "Molécule", "Dci1" },
                { "Forme", "Forme" }, { "Forme galénique", "Forme" },
                { "Dosage", "ShortName" }, { "Conditionnement", "ShortName" },
                { "Laboratoire", "Labo" }, { "Labo", "Labo" }, { "Fabricant", "Labo" },
                { "Prix", "Price" }, { "PU", "Price" }, { "Prix unitaire", "Price" },
                { "Voie", "Voie" }, { "Voie d'administration", "Voie" },
                { "Famille", "Fam1" }, { "Fam", "Fam1" },
                { "Spécialité", "Specialite" }, { "Specialite", "Specialite" }, { "Spécialite", "Specialite" },
            },

            // ── Liste Pharmacie : stock/inventaire officine ──
            EditionSourceType.ListePharmacie => new(StringComparer.OrdinalIgnoreCase)
            {
                { "Code", "PctCode" }, { "Code PCT", "PctCode" }, { "Code barre", "PctCode" },
                { "Désignation", "ItemName" }, { "Designation", "ItemName" },
                { "Produit", "ItemName" }, { "Nom", "ItemName" },
                { "DCI", "Dci1" }, { "D.C.I", "Dci1" },
                { "Forme", "Forme" },
                { "Voie", "Voie" }, { "Voie d'administration", "Voie" },
                { "Labo", "Labo" }, { "Laboratoire", "Labo" }, { "Fournisseur", "Labo" },
                { "Famille", "Fam1" }, { "Fam", "Fam1" },
                { "Spécialité", "Specialite" }, { "Specialite", "Specialite" }, { "Spécialite", "Specialite" },
                { "Prix", "Price" }, { "PPA", "Price" }, { "Prix public", "Price" },
                { "Prix-Réf", "RefPrice" }, { "Tarif de référence", "RefPrice" },
            },

            // ── ExcelSimple (défaut) : mapping flexible acceptant un maximum d'alias ──
            _ => new(StringComparer.OrdinalIgnoreCase)
            {
                { "Code PCT", "PctCode" }, { "Code_PCT", "PctCode" }, { "pctcode", "PctCode" },
                { "Code de la PCT", "PctCode" }, { "CodePCT", "PctCode" },
                { "Dénomination", "ItemName" }, { "Denomination", "ItemName" }, { "itemname", "ItemName" },
                { "Dénomination de base", "ItemName" }, { "Nom", "ItemName" },
                { "Désignation", "ShortName" }, { "Designation", "ShortName" }, { "shortname", "ShortName" },
                { "D.C.I", "Dci1" }, { "DCI", "Dci1" }, { "dci1", "Dci1" },
                { "D.C.J-Association", "DciAssociation" }, { "D.C.J Association", "DciAssociation" },
                { "DCI-Association", "DciAssociation" }, { "Association", "DciAssociation" },
                { "Forme", "Forme" },
                { "Tab.", "Tableau" }, { "Tab", "Tableau" },
                { "VEiC", "Veic" }, { "Catégorie VEIC", "Veic" },
                { "Labo.", "Labo" }, { "Labo", "Labo" }, { "Laboratoire", "Labo" },
                { "Prix-Réf", "RefPrice" }, { "Prix Réf", "RefPrice" }, { "Prix de référence", "RefPrice" },
                { "refprice", "RefPrice" }, { "PrixRef", "RefPrice" },
                { "Prix", "Price" }, { "Prix de vente public", "Price" },
                { "Remb.", "IsRemboursable" }, { "Remb", "IsRemboursable" },
                { "A.P.", "IsAp" }, { "A.P", "IsAp" }, { "Accord préalable", "IsAp" },
                { "Spécialité", "Specialite" }, { "specialite", "Specialite" },
                { "Famille", "Fam1" }, { "fam1", "Fam1" },
                { "Voie", "Voie" },
            },
        };
    }

    /// <summary>
    /// Mappe une ligne Excel vers un EditionRow
    /// </summary>
    private static EditionRow MapXlRowToEditionRow(IXLRow xlRow, Dictionary<int, string> columnMap, int lineNumber)
    {
        var row = new EditionRow { LineNumber = lineNumber };

        foreach (var (col, fieldName) in columnMap)
        {
            var cellValue = GetCellString(xlRow.Cell(col));

            switch (fieldName)
            {
                case "PctCode": row.PctCode = cellValue; break;
                case "ItemName": row.ItemName = cellValue; break;
                case "ShortName": row.ShortName = cellValue; break;
                case "Dci1": row.Dci1 = cellValue; break;
                case "DciAssociation": row.DciAssociation = cellValue; break;
                case "Forme": row.Forme = cellValue; break;
                case "Tableau": row.Tableau = cellValue; break;
                case "Veic": row.Veic = cellValue; break;
                case "Labo": row.Labo = cellValue; break;
                case "Fam1": row.Fam1 = cellValue; break;
                case "Specialite": row.Specialite = cellValue; break;
                case "Voie": row.Voie = cellValue; break;
                case "RefPrice": row.RefPrice = ParseInt(cellValue); break;
                case "Price": row.Price = ParseInt(cellValue); break;
                case "IsRemboursable": row.IsRemboursable = ParseInt(cellValue); break;
                case "IsAp": row.IsAp = ParseInt(cellValue); break;
            }
        }

        return row;
    }

    /// <summary>
    /// Tente de relier les lignes importées aux Medics existants par PctCode
    /// </summary>
    private async Task MatchExistingMedicsAsync(List<EditionRow> rows)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var pctCodes = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.PctCode))
            .Select(r => r.PctCode)
            .Distinct()
            .ToList();

        if (pctCodes.Count == 0) return;

        var existingMedics = await context.Medics
            .Where(m => pctCodes.Contains(m.pctcode))
            .Select(m => new { m.recordid, m.pctcode, m.medicid, m.price, m.refprice })
            .ToListAsync();

        // Tolerate duplicate PCT codes in the medic table (data integrity issue) instead of
        // crashing the whole import with "same key already added": first occurrence wins.
        var medicLookup = existingMedics.ToFirstWinsDictionary(m => m.pctcode);

        foreach (var row in rows)
        {
            if (!string.IsNullOrWhiteSpace(row.PctCode) && medicLookup.TryGetValue(row.PctCode, out var medic))
            {
                row.OriginalMedicRecordId = medic.recordid;
                row.MedicId = medic.medicid;
                row.RowStatus = RowStatus.Active;
                // Mark as already in the library — prevents ValidateAgainstLibraryAsync
                // from overwriting this with AjouterNew just because the row has
                // unknown field values (e.g. short CNAM category codes like "E", "I").
                row.ActionFlag = ActionFlag.Affecte;

                // Détecter les changements de prix
                if (row.Price != medic.price || row.RefPrice != medic.refprice)
                {
                    row.HasPriceChanged = true;
                }
            }
            else
            {
                row.RowStatus = RowStatus.New;
                row.ActionFlag = ActionFlag.AjouterNew;
            }
        }
    }

    /// <summary>
    /// Ajoute une valeur inconnue à la table de bibliothèque correspondante
    /// </summary>
    private async Task AddToLibraryAsync(string fieldName, string value)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        switch (fieldName)
        {
            case "Dci":
            case "DciAssociation":
            {
                // Une valeur composée « X+Y » est scindée en DCI distinctes
                // (règle métier : « + » sépare deux principes actifs). On insère
                // chaque segment séparément, en évitant les doublons canoniques.
                var segments = DciCompositeSplitter.Split(value);
                if (segments.Count == 0) break;

                var existingDcis = await context.Dcis
                    .Select(d => d.itemname)
                    .ToListAsync();
                var existingCanon = new HashSet<string>(existingDcis.Select(NameNormalizer.Canonical));

                var anyAdded = false;
                foreach (var seg in segments)
                {
                    if (existingCanon.Contains(NameNormalizer.Canonical(seg))) continue;
                    context.Dcis.Add(new Dci { itemname = seg });
                    existingCanon.Add(NameNormalizer.Canonical(seg));
                    anyAdded = true;
                }
                if (anyAdded) await context.SaveChangesAsync();
                break;
            }

            case "Labo":
                if (!await context.Labos.AnyAsync(l => l.itemname == value))
                {
                    context.Labos.Add(new Labos { itemname = value });
                    await context.SaveChangesAsync();
                }
                break;

            case "Fam1":
            case "Fam2":
            case "Fam3":
                if (!await context.Families.AnyAsync(f => f.itemname == value))
                {
                    context.Families.Add(new Families { itemname = value });
                    await context.SaveChangesAsync();
                }
                break;

            case "Forme":
            {
                // Match itemname OU abname (canonical) avant d'insérer un doublon.
                // Ex. "COMP" arrive depuis Excel : si formes.abname="COMP" existe déjà
                // (pour la forme Comprimé), on n'ajoute PAS une nouvelle ligne "COMP".
                var canonValue = NameNormalizer.Canonical(value);
                var formes = await context.Formes
                    .Select(f => new { f.itemname, f.abname })
                    .ToListAsync();
                var hit = formes.Any(f =>
                    NameNormalizer.Canonical(f.itemname) == canonValue ||
                    (!string.IsNullOrWhiteSpace(f.abname) && NameNormalizer.Canonical(f.abname) == canonValue));
                if (!hit)
                {
                    context.Formes.Add(new Formes { itemname = value });
                    await context.SaveChangesAsync();
                }
                break;
            }

            case "Voie":
            {
                var canonValue = NameNormalizer.Canonical(value);
                var voies = await context.Voies
                    .Select(v => new { v.itemname, v.abname })
                    .ToListAsync();
                var hit = voies.Any(v =>
                    NameNormalizer.Canonical(v.itemname) == canonValue ||
                    (!string.IsNullOrWhiteSpace(v.abname) && NameNormalizer.Canonical(v.abname) == canonValue));
                if (!hit)
                {
                    context.Voies.Add(new Voies { itemname = value });
                    await context.SaveChangesAsync();
                }
                break;
            }

            case "Specialite":
            {
                // Match itemname OU abname (canonical) avant d'insérer un doublon
                // (Spécialités porte une abréviation, comme Forme et Voie).
                var canonValue = NameNormalizer.Canonical(value);
                var specialites = await context.Specialites
                    .Select(s => new { s.itemname, s.abname })
                    .ToListAsync();
                var hit = specialites.Any(s =>
                    NameNormalizer.Canonical(s.itemname) == canonValue ||
                    (!string.IsNullOrWhiteSpace(s.abname) && NameNormalizer.Canonical(s.abname) == canonValue));
                if (!hit)
                {
                    context.Specialites.Add(new Specialites { itemname = value });
                    await context.SaveChangesAsync();
                }
                break;
            }
        }
    }

    /// <summary>
    /// Récupère la valeur d'un champ par son nom
    /// </summary>
    private static string GetFieldValue(EditionRow row, string fieldName) => fieldName switch
    {
        "Dci" => row.Dci1,
        "DciAssociation" => row.DciAssociation,
        "Labo" => row.Labo,
        "Fam1" => row.Fam1,
        "Fam2" => row.Fam2,
        "Fam3" => row.Fam3,
        "Forme" => row.Forme,
        "Voie" => row.Voie,
        "Specialite" => row.Specialite,
        _ => string.Empty
    };

    /// <summary>
    /// Mappe un EditionRow vers un nouveau Medic
    /// </summary>
    private static Medic MapEditionRowToMedic(EditionRow row) => new()
    {
        pctcode = row.PctCode,
        itemname = row.ItemName,
        shortname = row.ShortName,
        dci1 = row.Dci1,
        dci2 = row.Dci2,
        dci3 = row.Dci3,
        dci4 = row.Dci4,
        dci = row.DciAssociation,
        forme = row.Forme,
        voie = row.Voie,
        tableau = row.Tableau,
        veic = row.Veic,
        labo = row.Labo,
        fam1 = row.Fam1,
        fam2 = row.Fam2,
        fam3 = row.Fam3,
        specialite = row.Specialite,
        refprice = row.RefPrice,
        price = row.Price,
        isap = row.IsAp,
        isactive = 1
    };

    /// <summary>
    /// Met à jour un Medic existant depuis un EditionRow
    /// </summary>
    private static void UpdateMedicFromRow(Medic medic, EditionRow row)
    {
        medic.itemname = row.ItemName;
        medic.shortname = row.ShortName;
        medic.dci1 = row.Dci1;
        medic.dci = row.DciAssociation;
        medic.forme = row.Forme;
        medic.voie = row.Voie;
        medic.tableau = row.Tableau;
        medic.veic = row.Veic;
        medic.labo = row.Labo;
        medic.fam1 = row.Fam1;
        medic.specialite = row.Specialite;
        medic.refprice = row.RefPrice;
        medic.price = row.Price;
        medic.isap = row.IsAp;
    }

    private static string GetCellString(IXLCell cell)
    {
        if (cell.IsEmpty()) return string.Empty;
        return cell.GetString().Trim();
    }

    private static int ParseInt(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        // Handle decimal strings like "44.720" by parsing as double first
        if (double.TryParse(value, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var d))
        {
            return (int)Math.Round(d);
        }
        return 0;
    }

    // ─── Similarity search ────────────────────────────────────────────────

    // DCI hard-filter threshold
    private const int DciHardFilterThreshold = 85;

    // Stop-tokens stripped before name comparison
    private static readonly HashSet<string> _nameStopTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "comp", "comprime", "comprimes",
        "bt", "bte", "boite", "boites",
        "ml", "mg", "g", "ug", "µg", "mcg", "ui",
        "efferv", "effervescent", "effervescents",
        "sec", "secable",
        "flacon", "flacons",
        "gelule", "gelules",
        "amp", "ampoule", "ampoules",
        "sachet", "sachets",
        "sirop", "sirops",
        "patch", "patchs",
        "creme",
        "gel", "gels",
        "poudre", "poudres",
        "solution", "solutions",
        "suspension",
        "de", "du", "la", "le", "des", "les", "et", "en", "par", "pour"
    };

    // Regex for pure-digit or digit+unit tokens (e.g. "500", "500mg")
    private static readonly Regex _digitTokenRegex = new(@"^\d+[a-z]*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Regex for dose extraction
    private static readonly Regex _doseRegex = new(
        @"(\d+(?:[.,]\d+)?)\s*(mg|g|ml|µg|ug|mcg|ui|%)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Regex for collapsing whitespace
    private static readonly Regex _wsRegex = new(@"\s+", RegexOptions.Compiled);

    /// <summary>
    /// Normalizes a string: lowercase, strip diacritics, collapse whitespace.
    /// </summary>
    private static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var s = input.ToLowerInvariant();
        // Strip diacritics
        var decomposed = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        s = sb.ToString().Normalize(NormalizationForm.FormC);
        return _wsRegex.Replace(s, " ").Trim();
    }

    /// <summary>
    /// Strips noise tokens from a normalized name and joins remaining tokens.
    /// Returns empty string if no tokens survive.
    /// </summary>
    private static string StripNameTokens(string normalized)
    {
        if (string.IsNullOrWhiteSpace(normalized)) return string.Empty;
        var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var filtered = tokens.Where(t =>
            !_nameStopTokens.Contains(t) &&
            !_digitTokenRegex.IsMatch(t));
        return string.Join(" ", filtered);
    }

    /// <summary>
    /// Extracts a numeric dose and its unit from free text.
    /// </summary>
    private static (decimal? value, string unit) ExtractDose(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return (null, string.Empty);
        var m = _doseRegex.Match(text);
        if (!m.Success) return (null, string.Empty);
        if (decimal.TryParse(m.Groups[1].Value.Replace(',', '.'),
            NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
        {
            var u = m.Groups[2].Value.ToLowerInvariant();
            if (u == "ug" || u == "mcg") u = "µg"; // normalize microgram aliases
            return (v, u);
        }
        return (null, string.Empty);
    }

    /// <inheritdoc />
    public async Task<List<SimilarMedicResult>> SearchSimilarAsync(EditionRow row, int maxResults = 20)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // Load all active medics (name + dci + forme for matching)
        var medics = await context.Medics
            .Where(m => m.isactive == 1)
            .Select(m => new
            {
                m.recordid,
                m.itemname,
                m.dci1,
                m.forme,
                m.labo,
                m.voie,
                m.price
            })
            .ToListAsync();

        // ── Normalize search fields ───────────────────────────────────────
        var normSearchName  = Normalize(row.ItemName);
        var normSearchDci   = Normalize(row.Dci1);
        var normSearchForme = Normalize(row.Forme);
        var strippedSearch  = StripNameTokens(normSearchName);
        var (searchDoseVal, searchDoseUnit) = ExtractDose(normSearchName);
        var hasDci = !string.IsNullOrEmpty(normSearchDci);

        var scored = new List<SimilarMedicResult>(medics.Count);

        foreach (var m in medics)
        {
            var normMedicName  = Normalize(m.itemname);
            var normMedicDci   = Normalize(m.dci1);
            var normMedicForme = Normalize(m.forme);

            // ── 1. Hard pre-filter on DCI ────────────────────────────────
            int dciScore;
            if (hasDci)
            {
                dciScore = Fuzz.TokenSortRatio(normSearchDci, normMedicDci);
                if (dciScore < DciHardFilterThreshold) continue; // eliminates unrelated drugs instantly
            }
            else
            {
                dciScore = string.IsNullOrEmpty(normMedicDci) ? 50
                    : Fuzz.TokenSortRatio(normSearchName, normMedicDci);
            }

            // ── 2. Name score (with stop-token stripping + TokenSetRatio) ─
            var strippedMedic = StripNameTokens(normMedicName);
            int nameScore;
            if (string.IsNullOrEmpty(strippedSearch) || string.IsNullOrEmpty(strippedMedic))
                nameScore = 0;
            else
                nameScore = Fuzz.TokenSetRatio(strippedSearch, strippedMedic);

            // ── 3. Dose score ─────────────────────────────────────────────
            var (medicDoseVal, medicDoseUnit) = ExtractDose(normMedicName);
            int doseScore;
            if (searchDoseVal == null || medicDoseVal == null)
            {
                doseScore = 50; // neutral — missing on one or both sides
            }
            else if (searchDoseUnit == medicDoseUnit)
            {
                var larger = Math.Max(searchDoseVal.Value, medicDoseVal.Value);
                var diff   = Math.Abs(searchDoseVal.Value - medicDoseVal.Value);
                if (diff == 0)
                    doseScore = 100;
                else if (larger > 0 && diff / larger <= 0.10m)
                    doseScore = 70;
                else
                    doseScore = 0;
            }
            else
            {
                doseScore = 0;
            }

            // ── 4. Forme score ────────────────────────────────────────────
            int formeScore;
            if (string.IsNullOrEmpty(normSearchForme) && string.IsNullOrEmpty(normMedicForme))
                formeScore = 50;
            else if (string.IsNullOrEmpty(normSearchForme) || string.IsNullOrEmpty(normMedicForme))
                formeScore = 30;
            else
                formeScore = Fuzz.TokenSortRatio(normSearchForme, normMedicForme);

            // ── 5. Field-weighted combined score ──────────────────────────
            int combined;
            if (hasDci)
            {
                // dci 50% + dose 20% + forme 15% + name 15%
                combined = (int)Math.Round(
                    dciScore   * 0.50 +
                    doseScore  * 0.20 +
                    formeScore * 0.15 +
                    nameScore  * 0.15);
            }
            else
            {
                // name 45% + dose 30% + forme 25%
                combined = (int)Math.Round(
                    nameScore  * 0.45 +
                    doseScore  * 0.30 +
                    formeScore * 0.25);
            }

            if (combined >= 40) // Minimum relevance threshold
            {
                scored.Add(new SimilarMedicResult
                {
                    RecordId = m.recordid,
                    ItemName = m.itemname ?? string.Empty,
                    Dci      = m.dci1   ?? string.Empty,
                    Forme    = m.forme  ?? string.Empty,
                    Labo     = m.labo   ?? string.Empty,
                    Voie     = m.voie   ?? string.Empty,
                    Price    = m.price,
                    Score    = combined
                });
            }
        }

        return scored
            .OrderByDescending(r => r.Score)
            .Take(maxResults)
            .ToList();
    }

    // ─── Normalize / Dose helpers ─────────────────────────────────────────
}
