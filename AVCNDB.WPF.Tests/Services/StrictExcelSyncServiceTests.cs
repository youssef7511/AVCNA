using AVCNDB.WPF.Models;
using AVCNDB.WPF.Services;
using AVCNDB.WPF.Tests.Helpers;
using ClosedXML.Excel;
using FluentAssertions;
using System.IO;

namespace AVCNDB.WPF.Tests.Services;

public class StrictExcelSyncServiceTests : IDisposable
{
    private readonly DAL.AppDbContext _context;

    public StrictExcelSyncServiceTests()
    {
        _context = TestDbContextFactory.CreateSeededContext();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ValidateStrictAsync_UnexpectedColumns_FailsValidation()
    {
        var repo = new Repository<Dci>(_context);
        var excel = new ExcelService();
        var svc = new StrictExcelSyncService<Dci>(repo, excel);

        var headers = svc.ExpectedColumns.Concat(new[] { "unexpected" }).ToList();
        var path = CreateTempWorkbook("DCI", ws =>
        {
            WriteHeaders(ws, headers);
            ws.Cell(2, GetColumnIndex(headers, "itemname") + 1).Value = "X";
        });

        try
        {
            var result = await svc.ValidateStrictAsync(path);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("Colonnes non reconnues", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public async Task ValidateStrictAsync_DuplicateHeaders_FailsValidation()
    {
        var repo = new Repository<Dci>(_context);
        var excel = new ExcelService();
        var svc = new StrictExcelSyncService<Dci>(repo, excel);

        var duplicateHeader = svc.ExpectedColumns.First(c => c.Equals("itemname", StringComparison.OrdinalIgnoreCase));
        var headers = svc.ExpectedColumns.Concat(new[] { duplicateHeader }).ToList();

        var path = CreateTempWorkbook("DCI", ws =>
        {
            WriteHeaders(ws, headers);
            ws.Cell(2, GetColumnIndex(headers, "itemname") + 1).Value = "X";
        });

        try
        {
            var result = await svc.ValidateStrictAsync(path);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("En-têtes dupliqués", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public async Task ImportAndSyncAsync_DuplicateRecordIds_FailsValidation()
    {
        var repo = new Repository<Dci>(_context);
        var excel = new ExcelService();
        var svc = new StrictExcelSyncService<Dci>(repo, excel);

        var headers = svc.ExpectedColumns.ToList();
        var path = CreateTempWorkbook("DCI", ws =>
        {
            WriteHeaders(ws, headers);

            SetRow(ws, 2, headers, new Dictionary<string, object?>
            {
                ["recordid"] = 1,
                ["itemname"] = "A"
            });

            SetRow(ws, 3, headers, new Dictionary<string, object?>
            {
                ["recordid"] = 1,
                ["itemname"] = "B"
            });
        });

        try
        {
            var result = await svc.ImportAndSyncAsync(path, "DCI");

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("recordid dupliqués", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public async Task ImportAndSyncAsync_UpdatesExistingAndInsertsNew()
    {
        var repo = new Repository<Dci>(_context);
        var excel = new ExcelService();
        var svc = new StrictExcelSyncService<Dci>(repo, excel);

        var headers = svc.ExpectedColumns.ToList();
        var path = CreateTempWorkbook("DCI", ws =>
        {
            WriteHeaders(ws, headers);

            SetRow(ws, 2, headers, new Dictionary<string, object?>
            {
                ["recordid"] = 1,
                ["itemname"] = "Paracétamol (MAJ)"
            });

            // recordid left empty -> insert
            SetRow(ws, 3, headers, new Dictionary<string, object?>
            {
                ["itemname"] = "Nouvelle DCI"
            });
        });

        try
        {
            var result = await svc.ImportAndSyncAsync(path, "DCI");

            result.IsValid.Should().BeTrue();
            result.UpdatedCount.Should().Be(1);
            result.InsertedCount.Should().Be(1);
            result.SkippedCount.Should().Be(0);

            var updated = await repo.GetByIdAsync(1);
            updated.Should().NotBeNull();
            updated!.itemname.Should().Be("Paracétamol (MAJ)");

            var all = await repo.GetAllAsync();
            all.Should().Contain(d => d.itemname == "Nouvelle DCI");
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public async Task ImportAndSyncAsync_Families_UpdatesExistingAndInsertsNew()
    {
        var repo = new Repository<Families>(_context);
        var excel = new ExcelService();
        var svc = new StrictExcelSyncService<Families>(repo, excel);

        var headers = svc.ExpectedColumns.ToList();
        var path = CreateTempWorkbook("Familles", ws =>
        {
            WriteHeaders(ws, headers);

            SetRow(ws, 2, headers, new Dictionary<string, object?>
            {
                ["recordid"] = 1,
                ["itemname"] = "Antalgiques (MAJ)",
                ["subvalue"] = ""
            });

            SetRow(ws, 3, headers, new Dictionary<string, object?>
            {
                ["itemname"] = "Nouvelle famille",
                ["subvalue"] = ""
            });
        });

        try
        {
            var result = await svc.ImportAndSyncAsync(path, "Familles");

            result.IsValid.Should().BeTrue();
            result.UpdatedCount.Should().Be(1);
            result.InsertedCount.Should().Be(1);
            result.SkippedCount.Should().Be(0);

            var updated = await repo.GetByIdAsync(1);
            updated.Should().NotBeNull();
            updated!.itemname.Should().Be("Antalgiques (MAJ)");

            var all = await repo.GetAllAsync();
            all.Should().Contain(f => f.itemname == "Nouvelle famille");
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public async Task ImportAndSyncAsync_Families_DuplicateRecordIds_FailsValidation()
    {
        var repo = new Repository<Families>(_context);
        var excel = new ExcelService();
        var svc = new StrictExcelSyncService<Families>(repo, excel);

        var headers = svc.ExpectedColumns.ToList();
        var path = CreateTempWorkbook("Familles", ws =>
        {
            WriteHeaders(ws, headers);

            SetRow(ws, 2, headers, new Dictionary<string, object?>
            {
                ["recordid"] = 1,
                ["itemname"] = "A"
            });

            SetRow(ws, 3, headers, new Dictionary<string, object?>
            {
                ["recordid"] = 1,
                ["itemname"] = "B"
            });
        });

        try
        {
            var result = await svc.ImportAndSyncAsync(path, "Familles");

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("recordid dupliqués", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public async Task ImportAndSyncAsync_Labos_UpdatesExistingAndInsertsNew()
    {
        var repo = new Repository<Labos>(_context);
        var excel = new ExcelService();
        var svc = new StrictExcelSyncService<Labos>(repo, excel);

        var headers = svc.ExpectedColumns.ToList();
        var path = CreateTempWorkbook("Laboratoires", ws =>
        {
            WriteHeaders(ws, headers);

            SetRow(ws, 2, headers, new Dictionary<string, object?>
            {
                ["recordid"] = 1,
                ["itemname"] = "Sanofi (MAJ)",
                ["subvalue"] = "France"
            });

            SetRow(ws, 3, headers, new Dictionary<string, object?>
            {
                ["itemname"] = "Nouveau labo",
                ["subvalue"] = ""
            });
        });

        try
        {
            var result = await svc.ImportAndSyncAsync(path, "Laboratoires");

            result.IsValid.Should().BeTrue();
            result.UpdatedCount.Should().Be(1);
            result.InsertedCount.Should().Be(1);
            result.SkippedCount.Should().Be(0);

            var updated = await repo.GetByIdAsync(1);
            updated.Should().NotBeNull();
            updated!.itemname.Should().Be("Sanofi (MAJ)");

            var all = await repo.GetAllAsync();
            all.Should().Contain(l => l.itemname == "Nouveau labo");
        }
        finally
        {
            SafeDelete(path);
        }
    }

    [Fact]
    public async Task ImportAndSyncAsync_Labos_DuplicateRecordIds_FailsValidation()
    {
        var repo = new Repository<Labos>(_context);
        var excel = new ExcelService();
        var svc = new StrictExcelSyncService<Labos>(repo, excel);

        var headers = svc.ExpectedColumns.ToList();
        var path = CreateTempWorkbook("Laboratoires", ws =>
        {
            WriteHeaders(ws, headers);

            SetRow(ws, 2, headers, new Dictionary<string, object?>
            {
                ["recordid"] = 1,
                ["itemname"] = "A"
            });

            SetRow(ws, 3, headers, new Dictionary<string, object?>
            {
                ["recordid"] = 1,
                ["itemname"] = "B"
            });
        });

        try
        {
            var result = await svc.ImportAndSyncAsync(path, "Laboratoires");

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("recordid dupliqués", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            SafeDelete(path);
        }
    }

    private static string CreateTempWorkbook(string sheetName, Action<IXLWorksheet> configure)
    {
        var path = Path.Combine(Path.GetTempPath(), $"strict-sync-test-{Guid.NewGuid():N}.xlsx");
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet(sheetName);
        configure(ws);
        workbook.SaveAs(path);
        return path;
    }

    private static void WriteHeaders(IXLWorksheet ws, IReadOnlyList<string> headers)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }
    }

    private static void SetRow(IXLWorksheet ws, int row, IReadOnlyList<string> headers, IReadOnlyDictionary<string, object?> values)
    {
        foreach (var (key, value) in values)
        {
            var colIndex = GetColumnIndex(headers, key);
            if (colIndex < 0) continue;

            if (value != null)
            {
                var cell = ws.Cell(row, colIndex + 1);
                switch (value)
                {
                    case string s:
                        cell.Value = s;
                        break;
                    case int i:
                        cell.Value = i;
                        break;
                    case long l:
                        cell.Value = l;
                        break;
                    case decimal d:
                        cell.Value = d;
                        break;
                    case double db:
                        cell.Value = db;
                        break;
                    case DateTime dt:
                        cell.Value = dt;
                        break;
                    case bool b:
                        cell.Value = b;
                        break;
                    default:
                        cell.Value = value.ToString();
                        break;
                }
            }
        }
    }

    private static int GetColumnIndex(IReadOnlyList<string> headers, string headerName)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            if (headers[i].Equals(headerName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
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
