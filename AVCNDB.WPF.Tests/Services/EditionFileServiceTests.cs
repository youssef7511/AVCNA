using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.Models;
using AVCNDB.WPF.Services;
using AVCNDB.WPF.DAL;
using AVCNDB.WPF.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace AVCNDB.WPF.Tests.Services;

/// <summary>
/// Tests pour EditionFileService
/// </summary>
public class EditionFileServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IDbContextFactory<AppDbContext>> _contextFactoryMock;
    private readonly Mock<IUnknownDataDetectionService> _detectionMock;
    private readonly Mock<IExcelService> _excelMock;
    private readonly EditionFileService _service;

    public EditionFileServiceTests()
    {
        _context = TestDbContextFactory.CreateSeededContext();

        _contextFactoryMock = new Mock<IDbContextFactory<AppDbContext>>();
        _contextFactoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => TestDbContextFactory.CreateSeededContext());

        _detectionMock = new Mock<IUnknownDataDetectionService>();
        _excelMock = new Mock<IExcelService>();

        _service = new EditionFileService(
            _contextFactoryMock.Object,
            _detectionMock.Object,
            _excelMock.Object);
    }

    [Fact]
    public async Task ValidateAgainstLibraryAsync_WithUnknownFields_SetsActionFlag()
    {
        // Arrange
        var rows = new List<EditionRow>
        {
            new()
            {
                LineNumber = 1,
                PctCode = "999999",
                ItemName = "Test Drug",
                Dci1 = "UNKNOWN_DCI_XYZ",
                Labo = "Sanofi",
                Forme = "Comprimé"
            }
        };

        var reports = new List<DetectionReport>
        {
            new()
            {
                RowIndex = 0,
                FieldResults = new List<FieldDetectionResult>
                {
                    new() { FieldName = "Dci", ImportedValue = "UNKNOWN_DCI_XYZ", IsKnown = false, Score = 20 },
                    new() { FieldName = "Labo", ImportedValue = "Sanofi", IsKnown = true, Score = 100 },
                    new() { FieldName = "Forme", ImportedValue = "Comprimé", IsKnown = true, Score = 100 }
                }
            }
        };

        _detectionMock
            .Setup(d => d.DetectBatchAsync(It.IsAny<IReadOnlyList<EditionRow>>()))
            .ReturnsAsync(reports);

        // Act
        var unknownCount = await _service.ValidateAgainstLibraryAsync(rows);

        // Assert
        unknownCount.Should().Be(1);
        rows[0].UnknownFields.Should().Contain("Dci");
        rows[0].ActionFlag.Should().Be(ActionFlag.AjouterNew);
    }

    [Fact]
    public async Task ValidateAgainstLibraryAsync_AllKnown_NoActionFlag()
    {
        // Arrange
        var rows = new List<EditionRow>
        {
            new()
            {
                LineNumber = 1,
                ItemName = "Doliprane 500mg",
                Dci1 = "Paracétamol",
                Labo = "Sanofi",
                Forme = "Comprimé"
            }
        };

        var reports = new List<DetectionReport>
        {
            new()
            {
                RowIndex = 0,
                FieldResults = new List<FieldDetectionResult>
                {
                    new() { FieldName = "Dci", ImportedValue = "Paracétamol", IsKnown = true, Score = 100 },
                    new() { FieldName = "Labo", ImportedValue = "Sanofi", IsKnown = true, Score = 100 },
                    new() { FieldName = "Forme", ImportedValue = "Comprimé", IsKnown = true, Score = 100 }
                }
            }
        };

        _detectionMock
            .Setup(d => d.DetectBatchAsync(It.IsAny<IReadOnlyList<EditionRow>>()))
            .ReturnsAsync(reports);

        // Act
        var unknownCount = await _service.ValidateAgainstLibraryAsync(rows);

        // Assert
        unknownCount.Should().Be(0);
        rows[0].UnknownFields.Should().BeEmpty();
        rows[0].ActionFlag.Should().Be(ActionFlag.None);
    }

    [Fact]
    public async Task RejectRowAsync_SetsDesaffecteAndClearsUnknowns()
    {
        // Arrange
        var row = new EditionRow
        {
            LineNumber = 1,
            ActionFlag = ActionFlag.AjouterNew,
            UnknownFields = { "Dci", "Labo" }
        };

        // Act
        await _service.RejectRowAsync(row);

        // Assert
        row.ActionFlag.Should().Be(ActionFlag.Desaffecte);
        row.UnknownFields.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveSessionAsync_PersistsToDatabase()
    {
        // Arrange — use a single context for this test
        var dbName = Guid.NewGuid().ToString();
        var context = TestDbContextFactory.CreateInMemoryContext(dbName);
        var factoryMock = new Mock<IDbContextFactory<AppDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => TestDbContextFactory.CreateInMemoryContext(dbName));

        // Need a fresh service with this specific factory
        var service = new EditionFileService(factoryMock.Object, _detectionMock.Object, _excelMock.Object);

        var session = new EditionFileSession
        {
            filepath = "/test/file.xlsx",
            sourcetype = "ExcelCNAM",
            description = "Test session",
            totalrows = 100,
            unknownrows = 10,
            status = "InProgress"
        };

        // Act
        await service.SaveSessionAsync(session);

        // Assert
        using var verifyContext = TestDbContextFactory.CreateInMemoryContext(dbName);
        var saved = await verifyContext.EditionFileSessions.FirstOrDefaultAsync();
        saved.Should().NotBeNull();
        saved!.filepath.Should().Be("/test/file.xlsx");
        saved.totalrows.Should().Be(100);

        context.Dispose();
    }

    [Fact]
    public async Task ApproveRowAsync_AddsUnknownValuesToLibraryTables()
    {
        // Arrange — shared InMemory DB
        var dbName = Guid.NewGuid().ToString();
        var seedContext = TestDbContextFactory.CreateSeededContext(dbName);
        seedContext.Dispose();

        var factoryMock = new Mock<IDbContextFactory<AppDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => TestDbContextFactory.CreateInMemoryContext(dbName));

        var service = new EditionFileService(factoryMock.Object, _detectionMock.Object, _excelMock.Object);

        var row = new EditionRow
        {
            LineNumber = 1,
            PctCode = "PCT_NEW_999",
            ItemName = "FakeMed 500mg",
            Dci1 = "Flurbiproflex",       // Unknown DCI
            Labo = "BioNTech",             // Unknown Labo
            Forme = "Comprimé",            // Known Forme (exists in seed)
            Voie = "",                     // Empty = ignored
            UnknownFields = { "Dci", "Labo" },
            ActionFlag = ActionFlag.AjouterNew
        };

        // Act
        await service.ApproveRowAsync(row);

        // Assert — verify library tables
        using var verifyContext = TestDbContextFactory.CreateInMemoryContext(dbName);

        // DCI "Flurbiproflex" should be added
        var dciExists = await verifyContext.Dcis.AnyAsync(d => d.itemname == "Flurbiproflex");
        dciExists.Should().BeTrue("Flurbiproflex should be added to dcis table");

        // Labo "BioNTech" should be added
        var laboExists = await verifyContext.Labos.AnyAsync(l => l.itemname == "BioNTech");
        laboExists.Should().BeTrue("BioNTech should be added to labos table");

        // New Medic should be inserted (no OriginalMedicRecordId → insert)
        var medicExists = await verifyContext.Medics.AnyAsync(m => m.pctcode == "PCT_NEW_999");
        medicExists.Should().BeTrue("a new Medic should be inserted");

        // Row state should be cleaned up
        row.UnknownFields.Should().BeEmpty();
        row.ActionFlag.Should().Be(ActionFlag.Affecte);
        row.RowStatus.Should().Be(RowStatus.Modified);
    }

    [Fact]
    public async Task ApproveRowAsync_DoesNotDuplicateExistingLibraryValues()
    {
        // Arrange — shared InMemory DB with seed data containing "Sanofi"
        var dbName = Guid.NewGuid().ToString();
        var seedContext = TestDbContextFactory.CreateSeededContext(dbName);
        var initialLaboCount = await seedContext.Labos.CountAsync();
        seedContext.Dispose();

        var factoryMock = new Mock<IDbContextFactory<AppDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => TestDbContextFactory.CreateInMemoryContext(dbName));

        var service = new EditionFileService(factoryMock.Object, _detectionMock.Object, _excelMock.Object);

        var row = new EditionRow
        {
            LineNumber = 1,
            PctCode = "PCT_DUP_001",
            ItemName = "TestDrug",
            Labo = "Sanofi",
            UnknownFields = { "Labo" },
            ActionFlag = ActionFlag.AjouterNew
        };

        // Act
        await service.ApproveRowAsync(row);

        // Assert — Sanofi should NOT be duplicated
        using var verifyContext = TestDbContextFactory.CreateInMemoryContext(dbName);
        var laboCount = await verifyContext.Labos.CountAsync();
        laboCount.Should().Be(initialLaboCount, "existing values should not be duplicated");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
