using AVCNDB.WPF.Contracts.Services;
using AVCNDB.WPF.DAL;
using AVCNDB.WPF.Services;
using AVCNDB.WPF.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

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

        _service = new FuzzyDetectionService(factoryMock.Object);
    }

    [Fact]
    public void CheckValue_ExactMatch_ReturnsIsKnownTrue()
    {
        // Arrange
        var knownValues = new List<string> { "Paracétamol", "Ibuprofène", "Amoxicilline" };

        // Act
        var result = _service.CheckValue("Dci", "Paracétamol", knownValues);

        // Assert
        result.IsKnown.Should().BeTrue();
        result.Score.Should().Be(100);
        result.BestMatch.Should().Be("Paracétamol");
    }

    [Fact]
    public void CheckValue_SimilarValue_ReturnsIsKnownTrue()
    {
        // Arrange — slight typo but still >= 80 score
        var knownValues = new List<string> { "Paracétamol", "Ibuprofène" };

        // Act
        var result = _service.CheckValue("Dci", "PARACETAMOL", knownValues);

        // Assert
        result.IsKnown.Should().BeTrue();
        result.Score.Should().BeGreaterThanOrEqualTo(80);
    }

    [Fact]
    public void CheckValue_UnrelatedValue_ReturnsIsKnownFalse()
    {
        // Arrange — "XYZTESTUNKNOWN123" won't match anything
        var knownValues = new List<string> { "Paracétamol", "Ibuprofène", "Amoxicilline" };

        // Act
        var result = _service.CheckValue("Dci", "XYZTESTUNKNOWN123", knownValues);

        // Assert
        result.IsKnown.Should().BeFalse();
        result.Score.Should().BeLessThan(80);
    }

    [Fact]
    public void CheckValue_EmptyValue_ReturnsIsKnownTrue()
    {
        // Arrange — empty string should always be "known" (no false positive)
        var knownValues = new List<string> { "Paracétamol" };

        // Act
        var result = _service.CheckValue("Dci", "", knownValues);

        // Assert
        result.IsKnown.Should().BeTrue();
        result.Score.Should().Be(100);
    }

    [Fact]
    public void CheckValue_EmptyLibrary_ReturnsIsKnownFalse()
    {
        // Arrange — no references to compare against
        var knownValues = new List<string>();

        // Act
        var result = _service.CheckValue("Dci", "Paracétamol", knownValues);

        // Assert
        result.IsKnown.Should().BeFalse();
        result.Score.Should().Be(0);
    }

    [Fact]
    public void CheckValue_CaseInsensitive_ReturnsIsKnownTrue()
    {
        // Arrange
        var knownValues = new List<string> { "Sanofi", "Pfizer" };

        // Act
        var result = _service.CheckValue("Labo", "SANOFI", knownValues);

        // Assert
        result.IsKnown.Should().BeTrue();
        result.Score.Should().BeGreaterThanOrEqualTo(80);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
