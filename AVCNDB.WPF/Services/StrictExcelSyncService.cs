using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Helpers;
using AVCNDB.WPF.Models;

namespace AVCNDB.WPF.Services;

public class StrictExcelSyncService<T> : IStrictExcelSyncService<T> where T : class, ITrackable, new()
{
    private readonly IRepository<T> _repository;
    private readonly IExcelService _excelService;

    public StrictExcelSyncService(IRepository<T> repository, IExcelService excelService)
    {
        _repository = repository;
        _excelService = excelService;
        ExpectedColumns = ExcelTemplateHelper.GetStrictColumns<T>();
    }

    public IReadOnlyList<string> ExpectedColumns { get; }

    public async Task CreateTemplateAsync(string filePath, string sheetName)
    {
        await _excelService.ExportAsync(Array.Empty<T>(), filePath, sheetName);
    }

    public async Task<ExcelStrictImportResult> ValidateStrictAsync(string filePath)
    {
        var validation = await _excelService.ValidateFileAsync(filePath, ExpectedColumns);

        var result = new ExcelStrictImportResult
        {
            IsValid = validation.IsValid,
            Errors = validation.Errors,
            Warnings = validation.Warnings,
            RowCount = validation.RowCount,
            FoundColumns = validation.FoundColumns
        };

        var duplicateHeaders = ExcelTemplateHelper.GetDuplicateColumns(result.FoundColumns);
        if (duplicateHeaders.Any())
        {
            result.IsValid = false;
            result.Errors.Add($"En-têtes dupliqués : {string.Join(", ", duplicateHeaders)}");
        }

        var unexpected = ExcelTemplateHelper.GetUnexpectedColumns(result.FoundColumns, ExpectedColumns);
        if (unexpected.Any())
        {
            result.IsValid = false;
            result.Errors.Add($"Colonnes non reconnues (template strict) : {string.Join(", ", unexpected)}");
        }

        return result;
    }

    public async Task<ExcelStrictImportResult> ImportAndSyncAsync(string filePath, string sheetName)
    {
        var validation = await ValidateStrictAsync(filePath);
        if (!validation.IsValid)
        {
            return validation;
        }

        var imported = (await _excelService.ImportAsync<T>(filePath, sheetName)).ToList();

        var duplicateIds = imported
            .Where(x => x.recordid > 0)
            .GroupBy(x => x.recordid)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateIds.Any())
        {
            validation.IsValid = false;
            validation.Errors.Add($"recordid dupliqués dans le fichier : {string.Join(", ", duplicateIds)}");
            return validation;
        }

        var idsToUpdate = imported
            .Where(x => x.recordid > 0)
            .Select(x => x.recordid)
            .Distinct()
            .ToList();

        var existingById = new Dictionary<int, T>();
        if (idsToUpdate.Count > 0)
        {
            var existing = await _repository.FindAsync(x => idsToUpdate.Contains(x.recordid));
            existingById = existing.ToDictionary(x => x.recordid);
        }

        var now = DateTime.Now;
        var toInsert = new List<T>();

        var updatedCount = 0;
        var skippedCount = 0;

        var itemNameProperty = typeof(T).GetProperty("itemname");
        var hasItemName = itemNameProperty?.PropertyType == typeof(string) && itemNameProperty.CanRead;

        foreach (var row in imported)
        {
            if (hasItemName)
            {
                var name = (string?)itemNameProperty!.GetValue(row);
                if (string.IsNullOrWhiteSpace(name))
                {
                    skippedCount++;
                    continue;
                }
            }

            if (row.recordid > 0 && existingById.TryGetValue(row.recordid, out var current))
            {
                EntityCopyHelper.CopyWritableProperties(row, current, "recordid");
                current.updatedat = now;
                current.addedat ??= now;
                await _repository.UpdateAsync(current);
                updatedCount++;
            }
            else
            {
                row.recordid = 0;
                row.addedat ??= now;
                row.updatedat = now;
                toInsert.Add(row);
            }
        }

        if (toInsert.Count > 0)
        {
            await _repository.AddRangeAsync(toInsert);
        }

        validation.InsertedCount = toInsert.Count;
        validation.UpdatedCount = updatedCount;
        validation.SkippedCount = skippedCount;

        return validation;
    }
}
