using System.IO;
using System.Reflection;
using ClosedXML.Excel;
using AVCNDB.WPF.Contracts.Services;

namespace AVCNDB.WPF.Services;

/// <summary>
/// Service d'import/export Excel
/// </summary>
public class ExcelService : IExcelService
{
    public async Task<IEnumerable<T>> ImportAsync<T>(string filePath, string? sheetName = null) where T : class, new()
    {
        return await Task.Run(() =>
        {
            var result = new List<T>();
            
            using var workbook = new XLWorkbook(filePath);
            var worksheet = string.IsNullOrEmpty(sheetName) 
                ? workbook.Worksheets.First() 
                : workbook.Worksheet(sheetName);
            
            var properties = typeof(T).GetProperties()
                .Where(p => p.CanWrite)
                .ToList();
            
            // Lire les en-têtes de colonnes
            var headerRow = worksheet.Row(1);
            var columnMap = new Dictionary<int, PropertyInfo>();
            
            for (int col = 1; col <= headerRow.LastCellUsed()?.Address.ColumnNumber; col++)
            {
                var headerName = headerRow.Cell(col).GetString().Trim();
                var property = properties.FirstOrDefault(p => 
                    p.Name.Equals(headerName, StringComparison.OrdinalIgnoreCase));
                
                if (property != null)
                {
                    columnMap[col] = property;
                }
            }
            
            // Lire les données
            var dataRows = worksheet.RowsUsed().Skip(1);
            foreach (var row in dataRows)
            {
                var item = new T();
                
                foreach (var (col, property) in columnMap)
                {
                    var cell = row.Cell(col);
                    var value = ConvertCellValue(cell, property.PropertyType);
                    property.SetValue(item, value);
                }
                
                result.Add(item);
            }
            
            return result;
        });
    }

    public async Task ExportAsync<T>(IEnumerable<T> data, string filePath, string sheetName = "Data") where T : class
    {
        await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(sheetName);
            
            var properties = typeof(T).GetProperties().Where(p => p.CanRead).ToList();
            
            // En-têtes
            for (int i = 0; i < properties.Count; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = properties[i].Name;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1976D2");
                cell.Style.Font.FontColor = XLColor.White;
            }
            
            // Données
            var dataList = data.ToList();
            for (int row = 0; row < dataList.Count; row++)
            {
                var item = dataList[row];
                for (int col = 0; col < properties.Count; col++)
                {
                    var value = properties[col].GetValue(item);
                    var cell = worksheet.Cell(row + 2, col + 1);
                    
                    if (value != null)
                    {
                        cell.SetValue(value.ToString());
                    }
                }
            }
            
            // Ajuster les colonnes
            worksheet.Columns().AdjustToContents();
            
            // Appliquer un style de tableau
            var range = worksheet.RangeUsed();
            if (range != null)
            {
                range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }
            
            workbook.SaveAs(filePath);
        });
    }

    public async Task<byte[]> ExportToBytesAsync<T>(IEnumerable<T> data, string sheetName = "Data") where T : class
    {
        return await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(sheetName);
            
            var properties = typeof(T).GetProperties().Where(p => p.CanRead).ToList();
            
            // En-têtes
            for (int i = 0; i < properties.Count; i++)
            {
                worksheet.Cell(1, i + 1).Value = properties[i].Name;
            }
            
            // Données
            var dataList = data.ToList();
            for (int row = 0; row < dataList.Count; row++)
            {
                var item = dataList[row];
                for (int col = 0; col < properties.Count; col++)
                {
                    var value = properties[col].GetValue(item);
                    if (value != null)
                    {
                        worksheet.Cell(row + 2, col + 1).SetValue(value.ToString());
                    }
                }
            }
            
            worksheet.Columns().AdjustToContents();
            
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        });
    }

    public async Task<ExcelValidationResult> ValidateFileAsync(string filePath, IEnumerable<string> expectedColumns)
    {
        return await Task.Run(() =>
        {
            var result = new ExcelValidationResult { IsValid = true };
            
            try
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheets.First();
                
                // Récupérer les colonnes du fichier
                var headerRow = worksheet.Row(1);
                var foundColumns = new List<string>();
                
                for (int col = 1; col <= headerRow.LastCellUsed()?.Address.ColumnNumber; col++)
                {
                    foundColumns.Add(headerRow.Cell(col).GetString().Trim());
                }
                
                result.FoundColumns = foundColumns;
                result.RowCount = worksheet.RowsUsed().Count() - 1; // Moins l'en-tête
                
                // Vérifier les colonnes manquantes
                var expectedList = expectedColumns.ToList();
                result.MissingColumns = expectedList
                    .Where(c => !foundColumns.Any(f => f.Equals(c, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                
                if (result.MissingColumns.Any())
                {
                    result.IsValid = false;
                    result.Errors.Add($"Colonnes manquantes : {string.Join(", ", result.MissingColumns)}");
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Erreur lors de l'ouverture du fichier : {ex.Message}");
            }
            
            return result;
        });
    }

    private static object? ConvertCellValue(IXLCell cell, Type targetType)
    {
        if (cell.IsEmpty()) return null;
        
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        
        try
        {
            if (underlyingType == typeof(string))
                return cell.GetString();
            if (underlyingType == typeof(int))
                return cell.GetValue<int>();
            if (underlyingType == typeof(decimal))
                return cell.GetValue<decimal>();
            if (underlyingType == typeof(double))
                return cell.GetValue<double>();
            if (underlyingType == typeof(DateTime))
                return cell.GetDateTime();
            if (underlyingType == typeof(bool))
                return cell.GetBoolean();
            
            return Convert.ChangeType(cell.Value, underlyingType);
        }
        catch
        {
            return null;
        }
    }
}
