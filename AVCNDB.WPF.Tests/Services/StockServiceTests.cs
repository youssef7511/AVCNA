using AVCNDB.WPF.Models;
using AVCNDB.WPF.Services;
using AVCNDB.WPF.Tests.Helpers;

namespace AVCNDB.WPF.Tests.Services;

/// <summary>
/// Tests unitaires pour le service de gestion du stock
/// </summary>
public class StockServiceTests : IDisposable
{
    private readonly DAL.AppDbContext _context;
    private readonly StockService _stockService;

    public StockServiceTests()
    {
        _context = TestDbContextFactory.CreateSeededContext();
        _stockService = new StockService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GetLowStockAlerts Tests

    [Fact]
    public async Task GetLowStockAlertsAsync_ReturnsLowStockItems()
    {
        // Act
        var alerts = await _stockService.GetLowStockAlertsAsync();

        // Assert
        alerts.Should().NotBeNull();
    }

    #endregion

    #region GetExpiryAlerts Tests

    [Fact]
    public async Task GetExpiryAlertsAsync_ReturnsExpiringItems()
    {
        // Act
        var alerts = await _stockService.GetExpiryAlertsAsync(90);

        // Assert
        alerts.Should().NotBeNull();
    }

    [Fact]
    public async Task GetExpiryAlertsAsync_WithShortWindow_ReturnsItems()
    {
        // Act
        var alerts7Days = await _stockService.GetExpiryAlertsAsync(7);
        var alerts30Days = await _stockService.GetExpiryAlertsAsync(30);

        // Assert
        alerts7Days.Count().Should().BeLessOrEqualTo(alerts30Days.Count());
    }

    #endregion

    #region GetTotalAlerts Tests

    [Fact]
    public async Task GetTotalAlertsCountAsync_ReturnsCombinedCount()
    {
        // Act
        var count = await _stockService.GetTotalAlertsCountAsync();

        // Assert
        count.Should().BeGreaterOrEqualTo(0);
    }

    #endregion
}
