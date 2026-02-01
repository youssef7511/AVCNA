using AVCNDB.WPF.Models;
using AVCNDB.WPF.Services;
using AVCNDB.WPF.Tests.Helpers;

namespace AVCNDB.WPF.Tests.Services;

/// <summary>
/// Tests unitaires pour le Repository générique
/// </summary>
public class RepositoryTests : IDisposable
{
    private readonly DAL.AppDbContext _context;

    public RepositoryTests()
    {
        _context = TestDbContextFactory.CreateSeededContext();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllMedics()
    {
        // Arrange
        var repository = new Repository<Medic>(_context);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllDcis()
    {
        // Arrange
        var repository = new Repository<Dci>(_context);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(5);
        result.Should().Contain(d => d.itemname == "Paracétamol");
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsMedic()
    {
        // Arrange
        var repository = new Repository<Medic>(_context);

        // Act
        var result = await repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.itemname.Should().Be("Doliprane 500mg");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        var repository = new Repository<Medic>(_context);

        // Act
        var result = await repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_ValidMedic_AddsMedicToDatabase()
    {
        // Arrange
        var repository = new Repository<Medic>(_context);
        var newMedic = new Medic
        {
            recordid = 100,
            itemname = "Nouveau Médicament",
            barcode = "3400999999999",
            price = 5000
        };

        // Act
        var result = await repository.AddAsync(newMedic);

        // Assert
        result.Should().NotBeNull();
        result.recordid.Should().Be(100);
        
        var addedMedic = await repository.GetByIdAsync(100);
        addedMedic.Should().NotBeNull();
        addedMedic!.itemname.Should().Be("Nouveau Médicament");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingMedic_UpdatesMedic()
    {
        // Arrange
        var repository = new Repository<Medic>(_context);
        var medic = await repository.GetByIdAsync(1);
        medic!.itemname = "Doliprane Modifié";

        // Act
        await repository.UpdateAsync(medic);

        // Assert
        var updatedMedic = await repository.GetByIdAsync(1);
        updatedMedic!.itemname.Should().Be("Doliprane Modifié");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingMedic_RemovesMedic()
    {
        // Arrange
        var repository = new Repository<Medic>(_context);
        var medic = await repository.GetByIdAsync(1);

        // Act
        await repository.DeleteAsync(medic!);

        // Assert
        var deletedMedic = await repository.GetByIdAsync(1);
        deletedMedic.Should().BeNull();
    }

    #endregion

    #region FindAsync Tests

    [Fact]
    public async Task FindAsync_WithPredicate_ReturnsMatchingMedics()
    {
        // Arrange
        var repository = new Repository<Medic>(_context);

        // Act
        var result = await repository.FindAsync(m => m.itemname.Contains("Doliprane"));

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(m => m.itemname.Should().Contain("Doliprane"));
    }

    [Fact]
    public async Task FindAsync_NoMatches_ReturnsEmptyList()
    {
        // Arrange
        var repository = new Repository<Medic>(_context);

        // Act
        var result = await repository.FindAsync(m => m.itemname.Contains("Inexistant"));

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetPagedAsync Tests

    [Fact]
    public async Task GetPagedAsync_FirstPage_ReturnsCorrectItems()
    {
        // Arrange
        var repository = new Repository<Medic>(_context);

        // Act
        var result = await repository.GetPagedAsync(1, 2);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(3);
        result.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task GetPagedAsync_LastPage_ReturnsRemainingItems()
    {
        // Arrange
        var repository = new Repository<Medic>(_context);

        // Act
        var result = await repository.GetPagedAsync(3, 2);

        // Assert
        result.Items.Should().HaveCount(1);  // 5 items, page 3 with pageSize 2 = 1 item
        result.TotalPages.Should().Be(3);
    }

    #endregion

    #region CountAsync Tests

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var repository = new Repository<Medic>(_context);

        // Act
        var result = await repository.CountAsync();

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    public async Task CountAsync_WithPredicate_ReturnsFilteredCount()
    {
        // Arrange
        var repository = new Repository<Medic>(_context);

        // Act
        var result = await repository.CountAsync(m => m.family == "Antibiotiques");

        // Assert
        result.Should().Be(2);  // Amoxil et Augmentin
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WithExistingItems_ReturnsTrue()
    {
        // Arrange
        var repository = new Repository<Medic>(_context);

        // Act
        var result = await repository.ExistsAsync(m => m.barcode == "3400935262585");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNoMatchingItems_ReturnsFalse()
    {
        // Arrange
        var repository = new Repository<Medic>(_context);

        // Act
        var result = await repository.ExistsAsync(m => m.barcode == "0000000000000");

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
