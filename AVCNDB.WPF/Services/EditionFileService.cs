using System.Data;
using System.IO;
using ClosedXML.Excel;
using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.DAL;
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
            var rows = await Task.Run(() =>
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheets.First();

                var headerRow = worksheet.Row(1);
                var columnMap = BuildColumnMap(headerRow, sourceType);

                var editionRows = new List<EditionRow>();
                var dataRows = worksheet.RowsUsed().Skip(1); // skip header
                int lineNumber = 1;

                foreach (var xlRow in dataRows)
                {
                    var edRow = MapXlRowToEditionRow(xlRow, columnMap, lineNumber);
                    editionRows.Add(edRow);
                    lineNumber++;
                }

                return editionRows;
            });

            // Tenter de relier à des Medics existants par PctCode
            await MatchExistingMedicsAsync(rows);

            result.Success = true;
            result.Rows = rows;
            result.TotalRowsRead = rows.Count;
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
        // Ajouter les valeurs inconnues aux tables de bibliothèque
        foreach (var fieldName in row.UnknownFields)
        {
            var value = GetFieldValue(row, fieldName);
            if (!string.IsNullOrWhiteSpace(value))
            {
                await AddToLibraryAsync(fieldName, value);
            }
        }

        // Insérer ou mettre à jour le Medic
        using var context = await _contextFactory.CreateDbContextAsync();

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
            var newMedic = MapEditionRowToMedic(row);
            context.Medics.Add(newMedic);
        }

        await context.SaveChangesAsync();

        row.UnknownFields.Clear();
        row.NotifyUnknownFieldsChanged();
        row.ActionFlag = ActionFlag.Affecte;
        row.RowStatus = RowStatus.Modified;
    }

    /// <inheritdoc />
    public async Task RejectRowAsync(EditionRow row)
    {
        await Task.CompletedTask;
        row.ActionFlag = ActionFlag.Desaffecte;
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
    /// Construit le mapping colonnes Excel → champs EditionRow selon le type de source
    /// </summary>
    private static Dictionary<int, string> BuildColumnMap(IXLRow headerRow, EditionSourceType sourceType)
    {
        var map = new Dictionary<int, string>();
        var lastCol = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

        // Mapping flexible: reconnaît les noms français et anglais
        var fieldAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Code PCT
            { "Code PCT", "PctCode" }, { "Code_PCT", "PctCode" }, { "pctcode", "PctCode" },
            { "Code de la PCT", "PctCode" }, { "CodePCT", "PctCode" },
            
            // Dénomination
            { "Dénomination", "ItemName" }, { "Denomination", "ItemName" }, { "itemname", "ItemName" },
            { "Dénomination de base", "ItemName" }, { "Nom", "ItemName" },
            
            // Désignation (shortname)
            { "Désignation", "ShortName" }, { "Designation", "ShortName" }, { "shortname", "ShortName" },
            
            // DCI
            { "D.C.I", "Dci1" }, { "DCI", "Dci1" }, { "dci1", "Dci1" },
            { "D.C.J-Association", "DciAssociation" }, { "D.C.J Association", "DciAssociation" },
            { "DCI-Association", "DciAssociation" }, { "DCI-association", "DciAssociation" },
            { "dci", "DciAssociation" }, { "Association", "DciAssociation" },
            
            // Forme
            { "Forme", "Forme" }, { "forme", "Forme" },
            
            // Tableau
            { "Tab.", "Tableau" }, { "Tab", "Tableau" }, { "tableau", "Tableau" },
            
            // VEIC
            { "VEiC", "Veic" }, { "VEIC", "Veic" }, { "veic", "Veic" },
            { "Catégorie VEIC", "Veic" },
            
            // Labo
            { "Labo.", "Labo" }, { "Labo", "Labo" }, { "labo", "Labo" }, { "Laboratoire", "Labo" },
            
            // Prix
            { "Prix-Réf", "RefPrice" }, { "Prix Réf", "RefPrice" }, { "Prix de référence", "RefPrice" },
            { "refprice", "RefPrice" }, { "PrixRef", "RefPrice" },
            { "Prix", "Price" }, { "prix", "Price" }, { "Prix de vente public", "Price" },
            { "Remb.", "IsRemboursable" }, { "Remb", "IsRemboursable" },
            { "A.P.", "IsAp" }, { "A.P", "IsAp" }, { "Accord préalable", "IsAp" },
            
            // Spécialité / Familles
            { "Spécialité", "Specialite" }, { "specialite", "Specialite" },
            { "Famille", "Fam1" }, { "fam1", "Fam1" },
            { "Voie", "Voie" }, { "voie", "Voie" },
        };

        for (int col = 1; col <= lastCol; col++)
        {
            var header = headerRow.Cell(col).GetString().Trim();
            if (fieldAliases.TryGetValue(header, out var fieldName))
            {
                map[col] = fieldName;
            }
        }

        return map;
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

        var medicLookup = existingMedics.ToDictionary(m => m.pctcode, m => m);

        foreach (var row in rows)
        {
            if (!string.IsNullOrWhiteSpace(row.PctCode) && medicLookup.TryGetValue(row.PctCode, out var medic))
            {
                row.OriginalMedicRecordId = medic.recordid;
                row.MedicId = medic.medicid;
                row.RowStatus = RowStatus.Active;

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
                if (!await context.Dcis.AnyAsync(d => d.itemname == value))
                {
                    context.Dcis.Add(new Dci { itemname = value });
                    await context.SaveChangesAsync();
                }
                break;

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
                if (!await context.Formes.AnyAsync(f => f.itemname == value))
                {
                    context.Formes.Add(new Formes { itemname = value });
                    await context.SaveChangesAsync();
                }
                break;

            case "Voie":
                if (!await context.Voies.AnyAsync(v => v.itemname == value))
                {
                    context.Voies.Add(new Voies { itemname = value });
                    await context.SaveChangesAsync();
                }
                break;

            case "Specialite":
                if (!await context.Specialites.AnyAsync(s => s.itemname == value))
                {
                    context.Specialites.Add(new Specialites { itemname = value });
                    await context.SaveChangesAsync();
                }
                break;
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
}
