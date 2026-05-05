using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.DAL;
using AVCNDB.WPF.Models;
using AVCNDB.WPF.Services;
using AVCNDB.WPF.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AVCNDB.WPF.Tests.Services;

/// <summary>
/// Tests pour FuzzyDetectionService (seuil >= 80 = connu, < 80 = inconnu)
/// </summary>
public class FuzzyDetectionServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly FuzzyDetectionService _service;

    public FuzzyDetectionServiceTests()
    {
        _context = TestDbContextFactory.CreateSeededContext();

        // Create a factory that returns our test context
        var factoryMock = new Mock<IDbContextFactory<AppDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => TestDbContextFactory.CreateSeededContext());

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        _service = new FuzzyDetectionService(factoryMock.Object, configuration);
    }

    // ============================================
    // CheckValue — Exact & similar matches
    // ============================================

    [Fact]
    public void CheckValue_ExactMatch_ReturnsIsKnownTrue()
    {
        var knownValues = new List<string> { "Paracétamol", "Ibuprofène", "Amoxicilline" };

        var result = _service.CheckValue("Dci", "Paracétamol", knownValues);

        result.IsKnown.Should().BeTrue();
        result.Score.Should().Be(100);
        result.BestMatch.Should().Be("Paracétamol");
    }

    [Fact]
    public void CheckValue_SimilarValue_ReturnsIsKnownTrue()
    {
        var knownValues = new List<string> { "Paracétamol", "Ibuprofène" };

        var result = _service.CheckValue("Dci", "PARACETAMOL", knownValues);

        result.IsKnown.Should().BeTrue();
        result.Score.Should().BeGreaterThanOrEqualTo(80);
    }

    [Fact]
    public void CheckValue_CaseInsensitive_ReturnsIsKnownTrue()
    {
        var knownValues = new List<string> { "Sanofi", "Pfizer" };

        var result = _service.CheckValue("Labo", "SANOFI", knownValues);

        result.IsKnown.Should().BeTrue();
        result.Score.Should().BeGreaterThanOrEqualTo(80);
    }

    // ============================================
    // CheckValue — Unknown / edge cases
    // ============================================

    [Fact]
    public void CheckValue_UnrelatedValue_ReturnsIsKnownFalse()
    {
        var knownValues = new List<string> { "Paracétamol", "Ibuprofène", "Amoxicilline" };

        var result = _service.CheckValue("Dci", "XYZTESTUNKNOWN123", knownValues);

        result.IsKnown.Should().BeFalse();
        result.Score.Should().BeLessThan(80);
    }

    [Fact]
    public void CheckValue_EmptyValue_ReturnsIsKnownTrue()
    {
        var knownValues = new List<string> { "Paracétamol" };

        var result = _service.CheckValue("Dci", "", knownValues);

        result.IsKnown.Should().BeTrue();
        result.Score.Should().Be(100);
    }

    [Fact]
    public void CheckValue_WhitespaceOnly_ReturnsIsKnownTrue()
    {
        var knownValues = new List<string> { "Paracétamol" };

        var result = _service.CheckValue("Dci", "   ", knownValues);

        result.IsKnown.Should().BeTrue();
        result.Score.Should().Be(100);
    }

    [Fact]
    public void CheckValue_NullValue_ReturnsIsKnownTrue()
    {
        var knownValues = new List<string> { "Paracétamol" };

        var result = _service.CheckValue("Dci", null!, knownValues);

        result.IsKnown.Should().BeTrue();
        result.Score.Should().Be(100);
    }

    [Fact]
    public void CheckValue_EmptyLibrary_ReturnsIsKnownFalse()
    {
        var knownValues = new List<string>();

        var result = _service.CheckValue("Dci", "Paracétamol", knownValues);

        result.IsKnown.Should().BeFalse();
        result.Score.Should().Be(0);
    }

    // ============================================
    // CheckValue — Typos & near-misses
    // ============================================

    [Theory]
    [InlineData("Paracetamol", true)]   // missing accent → ~91
    [InlineData("Paracétamole", true)]  // extra letter → ~96
    [InlineData("Paractamol", true)]    // missing 'é' → ~90
    [InlineData("Parazetamol", true)]   // z instead of c + no accent → 82 (above threshold)
    [InlineData("Doliprane", false)]    // brand name ≠ DCI → low score
    public void CheckValue_TyposAndVariants_MatchesExpected(string input, bool expectedKnown)
    {
        var knownValues = new List<string> { "Paracétamol" };

        var result = _service.CheckValue("Dci", input, knownValues);

        result.IsKnown.Should().Be(expectedKnown,
            $"'{input}' vs 'Paracétamol' scored {result.Score} (threshold 80)");
    }

    [Theory]
    [InlineData("Comprimé", true)]
    [InlineData("COMPRIME", true)]      // uppercase, no accent
    [InlineData("Comprime", true)]      // no accent
    [InlineData("Comp.", false)]        // abbreviated — too short
    [InlineData("Gellule", false)]      // typo: double l on Gélule → 77 (below 80 threshold)
    public void CheckValue_FormeTypos_MatchesExpected(string input, bool expectedKnown)
    {
        var knownValues = new List<string> { "Comprimé", "Gélule", "Sirop", "Injectable" };

        var result = _service.CheckValue("Forme", input, knownValues);

        result.IsKnown.Should().Be(expectedKnown,
            $"'{input}' scored {result.Score}");
    }

    [Theory]
    [InlineData("Sanofi-Aventis", false)] // partial match on "Sanofi" → 60 (below threshold)
    [InlineData("Pfiser", true)]          // typo on Pfizer → ~83
    [InlineData("BioNTech", false)]       // not in library
    public void CheckValue_LaboTypos_MatchesExpected(string input, bool expectedKnown)
    {
        var knownValues = new List<string> { "Sanofi", "Pfizer", "Pharma 5" };

        var result = _service.CheckValue("Labo", input, knownValues);

        result.IsKnown.Should().Be(expectedKnown,
            $"'{input}' scored {result.Score}");
    }

    // ============================================
    // CheckValue — BestMatch correctness
    // ============================================

    [Fact]
    public void CheckValue_ReturnsBestMatchAmongMultiple()
    {
        var knownValues = new List<string> { "Amoxicilline", "Ampicilline", "Azithromycine" };

        var result = _service.CheckValue("Dci", "Amoxiciline", knownValues); // missing one l

        result.IsKnown.Should().BeTrue();
        result.BestMatch.Should().Be("Amoxicilline");
    }

    [Fact]
    public void CheckValue_ScorePrecision_NearThreshold()
    {
        // Test values that are expected to hover near the 80 threshold
        var knownValues = new List<string> { "Ibuprofène" };

        var result = _service.CheckValue("Dci", "Ibuprofene", knownValues);

        // Without accent but very close — should still be known
        result.IsKnown.Should().BeTrue();
        result.Score.Should().BeGreaterThanOrEqualTo(80);
    }

    // ============================================
    // DetectBatchAsync — Full pipeline (DB integration)
    // ============================================

    [Fact]
    public async Task DetectBatchAsync_AllKnownFields_ReportsAllKnown()
    {
        // Build row with values that exist in seeded test DB
        var row = new EditionRow
        {
            Dci1 = "Paracétamol",
            DciAssociation = "Ibuprofène",
            Labo = "Sanofi",
            Forme = "Comprimé",
            Fam1 = "Antalgiques",
            Voie = "",        // empty = known
            Specialite = "",  // empty = known
        };

        var reports = await _service.DetectBatchAsync(new[] { row });

        reports.Should().HaveCount(1);
        reports[0].AllFieldsKnown.Should().BeTrue();
        reports[0].UnknownFieldNames.Should().BeEmpty();
    }

    [Fact]
    public async Task DetectBatchAsync_UnknownDci_ReportsUnknown()
    {
        var row = new EditionRow
        {
            Dci1 = "XYZINVENTEDDRUG",
            DciAssociation = "",
            Labo = "Sanofi",
            Forme = "Comprimé",
            Fam1 = "Antalgiques",
            Voie = "",
            Specialite = "",
        };

        var reports = await _service.DetectBatchAsync(new[] { row });

        reports[0].AllFieldsKnown.Should().BeFalse();
        reports[0].UnknownFieldNames.Should().Contain("Dci");
    }

    [Fact]
    public async Task DetectBatchAsync_MultipleUnknown_ReportsAllUnknownFields()
    {
        var row = new EditionRow
        {
            Dci1 = "UNKNOWNDRUG",
            DciAssociation = "",
            Labo = "UNKNOWNLAB",
            Forme = "UNKNOWNFORME",
            Fam1 = "UNKNOWNFAMILY",
            Voie = "",
            Specialite = "",
        };

        var reports = await _service.DetectBatchAsync(new[] { row });

        reports[0].AllFieldsKnown.Should().BeFalse();
        reports[0].UnknownFieldNames.Should().Contain("Dci");
        reports[0].UnknownFieldNames.Should().Contain("Labo");
        reports[0].UnknownFieldNames.Should().Contain("Forme");
        reports[0].UnknownFieldNames.Should().Contain("Fam1");
    }

    [Fact]
    public async Task DetectBatchAsync_MultiplRows_ReturnsReportPerRow()
    {
        var rows = new[]
        {
            new EditionRow { Dci1 = "Paracétamol", Labo = "Sanofi", Forme = "Comprimé", Fam1 = "Antalgiques" },
            new EditionRow { Dci1 = "INVENTED123", Labo = "UNKNOWNLAB", Forme = "Sirop", Fam1 = "" },
            new EditionRow { Dci1 = "Ibuprofène", Labo = "Pfizer", Forme = "BADFORM", Fam1 = "Anti-inflammatoires" },
        };

        var reports = await _service.DetectBatchAsync(rows);

        reports.Should().HaveCount(3);
        reports[0].AllFieldsKnown.Should().BeTrue("row 1 has all known values");
        reports[1].UnknownFieldNames.Should().Contain("Dci", "row 2 DCI is invented");
        reports[1].UnknownFieldNames.Should().Contain("Labo", "row 2 lab is unknown");
        reports[2].UnknownFieldNames.Should().Contain("Forme", "row 3 forme is bad");
        reports[2].FieldResults.First(f => f.FieldName == "Dci").IsKnown.Should().BeTrue("row 3 DCI is valid");
    }

    [Fact]
    public async Task DetectBatchAsync_EmptyRow_AllFieldsKnown()
    {
        // Completely empty row — all fields empty = all known (no false positives)
        var row = new EditionRow();

        var reports = await _service.DetectBatchAsync(new[] { row });

        reports[0].AllFieldsKnown.Should().BeTrue();
    }

    // ============================================
    // DetectionReport model tests
    // ============================================

    [Fact]
    public void DetectionReport_UnknownFieldNames_FiltersCorrectly()
    {
        var report = new DetectionReport
        {
            RowIndex = 0,
            FieldResults = new List<FieldDetectionResult>
            {
                new() { FieldName = "Dci", IsKnown = true, Score = 100 },
                new() { FieldName = "Labo", IsKnown = false, Score = 40 },
                new() { FieldName = "Forme", IsKnown = true, Score = 95 },
                new() { FieldName = "Fam1", IsKnown = false, Score = 20 },
            }
        };

        report.AllFieldsKnown.Should().BeFalse();
        report.UnknownFieldNames.Should().BeEquivalentTo(new[] { "Labo", "Fam1" });
    }

    [Fact]
    public void FieldDetectionResult_Defaults_AreCorrect()
    {
        var result = new FieldDetectionResult();

        result.FieldName.Should().BeEmpty();
        result.ImportedValue.Should().BeEmpty();
        result.IsKnown.Should().BeFalse();
        result.Score.Should().Be(0);
        result.BestMatch.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
