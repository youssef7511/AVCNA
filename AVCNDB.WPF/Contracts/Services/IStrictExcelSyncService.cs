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
}
