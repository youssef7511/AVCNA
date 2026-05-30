using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.Contracts.Services;

public interface IStrictExcelSyncService<T> where T : class, ITrackable, new()
{
    IReadOnlyList<string> ExpectedColumns { get; }

    Task<ExcelStrictImportResult> ValidateStrictAsync(string filePath);

    Task CreateTemplateAsync(string filePath, string sheetName);

    Task<ExcelStrictImportResult> ImportAndSyncAsync(string filePath, string sheetName);
}

public class ExcelStrictImportResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public int RowCount { get; set; }
    public List<string> FoundColumns { get; set; } = new();

    public int InsertedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }

    /// <summary>
    /// Nombre de lignes reconnues comme doublons par normalisation canonique
    /// (même nom à la casse / aux accents / aux espaces près) : fusionnées avec une
    /// entrée existante ou ignorées comme doublon interne au fichier, au lieu d'être ajoutées.
    /// </summary>
    public int CanonicalDuplicatesCount { get; set; }

    /// <summary>
    /// Construit le récapitulatif affiché dans le dialogue de succès d'import.
    /// Ajoute une ligne explicite lorsque des doublons ont été détectés par normalisation.
    /// </summary>
    public string BuildImportSummary()
    {
        var summary =
            $"Lignes lues : {RowCount}" +
            $"\nInsérés : {InsertedCount}" +
            $"\nMis à jour : {UpdatedCount}" +
            $"\nIgnorés : {SkippedCount}";

        if (CanonicalDuplicatesCount > 0)
        {
            summary +=
                $"\n\nDoublons détectés par normalisation : {CanonicalDuplicatesCount}" +
                "\n(noms identiques à la casse et aux accents près — fusionnés avec l'entrée " +
                "existante au lieu d'être ajoutés en double).";
        }

        return summary;
    }
}
