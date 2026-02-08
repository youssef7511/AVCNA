using AVCNDB.WPF.Services;
using AVCNDB.WPF.Models;
using ClosedXML.Excel;
using System.IO;
using Xunit;

namespace AVCNDB.WPF.Tests.Services;

public class ExcelServiceTests
{
    [Fact]
    public async Task ValidateFileAsync_FlagsMissingColumns()
    {
        var path = CreateTempWorkbook(ws =>
        {
            ws.Cell(1, 1).Value = "itemname";
            ws.Cell(2, 1).Value = "X";
        });

        try
        {
            var svc = new ExcelService();
            var result = await svc.ValidateFileAsync(path, new[] { "itemname", "dci" });

            Assert.False(result.IsValid);
            Assert.Contains("dci", result.MissingColumns, StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public async Task ImportAsync_EmptyCell_DoesNotThrow_ForNonNullableInt()
    {
        var path = CreateTempWorkbook(ws =>
        {
            ws.Cell(1, 1).Value = "recordid";
            ws.Cell(1, 2).Value = "itemname";
            ws.Cell(1, 3).Value = "price";

            ws.Cell(2, 1).Value = 1;
            ws.Cell(2, 2).Value = "Test";
            // price left empty on purpose
        });

        try
        {
            var svc = new ExcelService();
            var items = (await svc.ImportAsync<Medic>(path)).ToList();

            Assert.Single(items);
            Assert.Equal(1, items[0].recordid);
            Assert.Equal("Test", items[0].itemname);
            Assert.Equal(0, items[0].price); // default(int)
        }
        finally
        {
            SafeDelete(path);
        }
    }

    private static string CreateTempWorkbook(Action<IXLWorksheet> configure)
    {
        var path = Path.Combine(Path.GetTempPath(), $"excel-test-{Guid.NewGuid():N}.xlsx");
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Sheet1");
        configure(ws);
        workbook.SaveAs(path);
        return path;
    }

    private static void SafeDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // ignore
        }
    }
}
