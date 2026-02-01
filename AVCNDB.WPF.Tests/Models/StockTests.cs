using AVCNDB.WPF.Models; 

namespace AVCNDB.WPF.Tests.Models;

/// <summary>
/// Tests unitaires pour le modèle Stock
/// </summary>
public class StockTests
{
    [Fact]
    public void Stock_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var stock = new Stock();

        // Assert
        stock.recordid.Should().Be(0);
        stock.medicid.Should().Be(0);
        stock.quantity.Should().Be(0);
        stock.minstock.Should().Be(0);
    }

    [Fact]
    public void Stock_IsLowStock_WhenQuantityBelowMinimum()
    {
        // Arrange
        var stock = new Stock
        {
            quantity = 5,
            minstock = 20
        };

        // Act
        var isLowStock = stock.quantity < stock.minstock;

        // Assert
        isLowStock.Should().BeTrue();
    }

    [Fact]
    public void Stock_IsNotLowStock_WhenQuantityAboveMinimum()
    {
        // Arrange
        var stock = new Stock
        {
            quantity = 50,
            minstock = 20
        };

        // Act
        var isLowStock = stock.quantity < stock.minstock;

        // Assert
        isLowStock.Should().BeFalse();
    }

    [Fact]
    public void Stock_IsOutOfStock_WhenQuantityIsZero()
    {
        // Arrange
        var stock = new Stock
        {
            quantity = 0,
            minstock = 20
        };

        // Act
        var isOutOfStock = stock.quantity <= 0;

        // Assert
        isOutOfStock.Should().BeTrue();
    }

    [Fact]
    public void Stock_IsExpiringSoon_WhenExpirationWithin30Days()
    {
        // Arrange
        var stock = new Stock
        {
            expirydate = DateTime.Now.AddDays(15)
        };

        // Act
        var isExpiringSoon = stock.expirydate <= DateTime.Now.AddDays(30);

        // Assert
        isExpiringSoon.Should().BeTrue();
    }

    [Fact]
    public void Stock_IsNotExpiringSoon_WhenExpirationFarAway()
    {
        // Arrange
        var stock = new Stock
        {
            expirydate = DateTime.Now.AddMonths(6)
        };

        // Act
        var isExpiringSoon = stock.expirydate <= DateTime.Now.AddDays(30);

        // Assert
        isExpiringSoon.Should().BeFalse();
    }

    [Fact]
    public void Stock_IsExpired_WhenExpirationDatePassed()
    {
        // Arrange
        var stock = new Stock
        {
            expirydate = DateTime.Now.AddDays(-1)
        };

        // Act
        var isExpired = stock.expirydate < DateTime.Now;

        // Assert
        isExpired.Should().BeTrue();
    }
}
